using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Plugins;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Database;
using FreeRP.ServerCore.User;
using FreeRP.Settings;
using FreeRP.User;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FreeRP.ServerCore.Plugin
{
    public class FrpPluginService : IFrpPluginService
    {
        private readonly ConcurrentDictionary<string, FrpPlugin> _allPlugins = [];
        private readonly ConcurrentDictionary<string, FrpMemberUsePlugin> _allMemberUsePlugin = [];

        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpDatabase _db;
        private readonly ILogger _log;
        private readonly FrpSettings _frpSettings;
        private readonly IFrpAuthService _systemUser;

        public FrpPluginService(IFrpDataService ds, FrpSettings frpSettings, IFrpLogService logService, FrpDatabase db, ILogger logger)
        {
            _ds = ds;
            _logService = logService;
            _db = db;
            _log = logger;
            _frpSettings = frpSettings;
            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };
        }

        internal async Task InitAsync()
        {
            await LoadPluginsAsync();
        }

        private async Task LoadPluginsAsync()
        {
            try
            {
                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var plugs = await db.ListOrDefaultAsync(x => x.RecordType == IFrpPluginService.RecordTypePlugin);
                if (plugs.Any())
                {
                    foreach (var p in plugs)
                    {
                        if (GrpcJson.GetModel<FrpPlugin>(p.DataAsJson) is FrpPlugin a)
                        {
                            _allPlugins[a.PluginId] = a;
                        }
                    }
                }

                var mups = await db.ListOrDefaultAsync(x => x.RecordType == IFrpPluginService.RecordTypeMemberUsePlugin);
                if (plugs.Any())
                {
                    foreach (var p in plugs)
                    {
                        if (GrpcJson.GetModel<FrpMemberUsePlugin>(p.DataAsJson) is FrpMemberUsePlugin a)
                        {
                            _allMemberUsePlugin[a.PluginId] = a;
                        }
                    }
                }

                await db.CloseDatabaseAsync();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(LoadPluginsAsync), ex, _systemUser);
                _log.LogError(ex, "Load users from Database");
            }
        }

        #region Get

        public ValueTask<IEnumerable<FrpPlugin>> GetAllPluginsAsync() => ValueTask.FromResult(_allPlugins.Values.AsEnumerable());

        public ValueTask<IEnumerable<FrpPlugin>> GetAllUserPluginsAsync(IFrpAuthService auth)
        {
            List<FrpPlugin> list = [];

            foreach (var item in _allMemberUsePlugin)
            {
                if (item.Value.MemberIsUser && item.Value.MemberId == auth.User.UserId)
                {
                    list.Add(_allPlugins[item.Value.PluginId]);
                }
                else if (auth.Roles.FirstOrDefault(x => x.RoleId == item.Value.MemberId) is not null)
                {
                    list.Add(_allPlugins[item.Value.PluginId]);
                }
            }

            return ValueTask.FromResult(list.AsEnumerable());
        }

        #endregion

        #region Add, change, delete

        public async ValueTask<FrpResponse> AddPluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            try
            {
                if (IsAdminOrSystemUser(auth.User) is FrpResponse eres)
                    return eres;

                if (_allPlugins.ContainsKey(plugin.PluginId))
                    return FrpResponse.Create(FrpErrorType.ErrorPluginExist, auth.I18n);

                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var r = IFrpRecordContext.GetRecord(auth.User.UserId, plugin.PluginId, IFrpUserService.RecordTypeUser, auth.User.UserId, GrpcJson.GetJson(plugin));
                await db.AddAsync(r);
                await db.SaveChangesAndCloseAsync();

                _allPlugins[plugin.PluginId] = plugin;
                await AddLogAsync(IFrpLogService.ActionAdd, r, auth);

                return FrpResponse.ErrorNone();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddPluginAsync), ex, auth);
                _log.LogError(ex, nameof(AddPluginAsync));
                return FrpResponse.ErrorUnknown(auth.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangePluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            try
            {
                if (IsAdminOrSystemUser(auth.User) is FrpResponse eres)
                    return eres;

                if (_allPlugins.TryGetValue(plugin.PluginId, out var p))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == plugin.PluginId && x.RecordType == IFrpPluginService.RecordTypePlugin);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);
                        await db.AddAsync(
                        IFrpRecordContext.GetRecord(auth.User.UserId, plugin.PluginId, IFrpUserService.RecordTypeUser, r.Owner, GrpcJson.GetJson(plugin)));
                        await db.SaveChangesAndCloseAsync();

                        _allPlugins[plugin.PluginId] = plugin;
                        await AddLogAsync(IFrpLogService.ActionChange, r, auth);

                        return FrpResponse.ErrorNone();
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorPluginNotExist, auth.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangePluginAsync), ex, auth);
                _log.LogError(ex, nameof(ChangePluginAsync));
                return FrpResponse.ErrorUnknown(auth.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeletePluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            try
            {
                if (IsAdminOrSystemUser(auth.User) is FrpResponse eres)
                    return eres;

                if (_allPlugins.TryRemove(plugin.PluginId, out _))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == plugin.PluginId && x.RecordType == IFrpPluginService.RecordTypePlugin);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);
                        await db.SaveChangesAndCloseAsync();

                        _allPlugins[plugin.PluginId] = plugin;
                        await AddLogAsync(IFrpLogService.ActionDelete, r, auth);

                        return FrpResponse.ErrorNone();
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorPluginNotExist, auth.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangePluginAsync), ex, auth);
                _log.LogError(ex, nameof(ChangePluginAsync));
                return FrpResponse.ErrorUnknown(auth.I18n.Text.ErrorUnknown);
            }
        }

        #endregion

        #region Helpers

        private FrpResponse? IsAdminOrSystemUser(FrpUser u)
        {
            if (u.Email == _frpSettings.Admin.Email || u.Email == _frpSettings.System.Email)
                return new FrpResponse() { ErrorType = FrpErrorType.ErrorAccessDenied };

            return null;
        }

        #endregion

        private async ValueTask AddLogAsync(string action, FrpRecord old, IFrpAuthService authService, string val1 = "", string val2 = "", string val3 = "", string val4 = "", string val5 = "")
        {
            await _logService.AddLogAsync(new()
            {
                Action = action,
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RecordId = old.RecordId,
                Record = Json.GetJson(old),
                RecordType = old.RecordType,
                DatabaseId = _frpSettings.DatabaseSettings.DatabaseId,
                Val1 = val1,
                Val2 = val2,
                Val3 = val3,
                Val4 = val4,
                Val5 = val5
            }, authService);
        }

        private async ValueTask AddExceptionAsync(string location, Exception ex, IFrpAuthService authService)
        {
            await _logService.AddExceptionLogAsync(new()
            {
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Location = $"{nameof(FrpPluginService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
