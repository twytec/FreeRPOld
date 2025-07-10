using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Log;
using FreeRP.Role;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Database;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FreeRP.ServerCore.Role
{
    public class FrpRoleService : IFrpRoleService
    {
        private readonly ConcurrentDictionary<string, FrpRole> _roles = [];
        private readonly ConcurrentDictionary<string, FrpUserInRole> _userInRoles = [];

        private readonly FrpSettings _frpSettings;
        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpDatabase _db;
        private readonly ILogger _log;
        private readonly IFrpAuthService _systemUser;

        public FrpRoleService(IFrpDataService ds, FrpSettings frpSettings, IFrpLogService logService, FrpDatabase db, ILogger logger)
        {
            _frpSettings = frpSettings;
            _ds = ds;
            _logService = logService;
            _db = db;
            _log = logger;

            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };
        }

        internal async Task InitAsync()
        {
            await LoadRoles();
        }

        private async Task LoadRoles()
        {
            try
            {
                var db = FrpRecordContextFactory.Create(_frpSettings, _db);

                var roles = await db.ListOrDefaultAsync(x => x.RecordType == IFrpRoleService.RecordTypeRole);
                if (roles.Any())
                {
                    foreach (var r in roles)
                    {
                        if (GrpcJson.GetModel<FrpRole>(r.DataAsJson) is FrpRole role)
                        {
                            _roles[r.RecordId] = role;
                        }
                    }
                }

                var uirs = await db.ListOrDefaultAsync(x => x.RecordType == IFrpRoleService.RecordTypeUserInRole);
                if (uirs.Any())
                {
                    foreach (var item in uirs)
                    {
                        if (GrpcJson.GetModel<FrpUserInRole>(item.DataAsJson) is FrpUserInRole uir)
                        {
                            _userInRoles[item.RecordId] = uir;
                        }
                    }
                }

                await db.CloseDatabaseAsync();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(LoadRoles), ex, _systemUser);
                _log.LogError(ex, "Can not load roles");
            }
        }

        #region Get

        public ValueTask<bool> IsRoleByIdExistsAsync(string id) => ValueTask.FromResult(_roles.ContainsKey(id));
        public ValueTask<bool> IsRoleByNameExistsAsync(string name) => ValueTask.FromResult(_roles.Values.FirstOrDefault(x => x.Name == name) is not null);
        public ValueTask<bool> IsUserInRoleAsync(string userId, string roleId) => ValueTask.FromResult(_userInRoles.ContainsKey(userId + roleId));

        public ValueTask<FrpRole?> GetRoleByIdAsync(string id)
        {
            var data = _roles.TryGetValue(id, out var role) ? role : null;
            return ValueTask.FromResult(data);
        }
        public ValueTask<IEnumerable<FrpRole>> GetAllRolesAsync() => ValueTask.FromResult(_roles.Values.AsEnumerable());

        public ValueTask<IEnumerable<FrpUserInRole>> GetAllUserInRolesAsync() => ValueTask.FromResult(_userInRoles.Values.AsEnumerable());

        public async ValueTask<IEnumerable<FrpRole>> GetUserRolesAsync(string userId)
        {
            try
            {
                return (from r in _roles
                        from ur in _userInRoles
                        where ur.Value.UserId == userId
                        where r.Value.RoleId == ur.Value.RoleId
                        select r.Value).AsEnumerable();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(GetUserRolesAsync), ex, _systemUser);
                _log.LogError(ex, nameof(GetUserRolesAsync));
                return [];
            }
        }

        #endregion

        public async ValueTask<FrpResponse> AddRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin is false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (string.IsNullOrWhiteSpace(role.Name))
                    return FrpResponse.Create(FrpErrorType.ErrorRoleNameRequired, authService.I18n);

                var ava = _roles.Where(x => x.Value.Name.Equals(role.Name, StringComparison.CurrentCultureIgnoreCase));
                if (ava.Any() is false)
                {
                    role.RoleId = FrpId.NewId();
                    _roles[role.RoleId] = role;

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = IFrpRecordContext.GetRecord(authService.User.UserId, role.RoleId, IFrpRoleService.RecordTypeRole, authService.User.UserId, GrpcJson.GetJson(role));
                    await db.AddAsync(r);
                    await db.SaveChangesAndCloseAsync();

                    await AddLogAsync(IFrpLogService.ActionAdd, r, authService);

                    return FrpResponse.Create(role);
                }
                else
                {
                    return FrpResponse.Create(FrpErrorType.ErrorRoleExist, authService.I18n);
                }
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddRoleAsync), ex, authService);
                _log.LogError(ex, nameof(AddRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin is false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (string.IsNullOrWhiteSpace(role.RoleId))
                    return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);

                var e = _roles.Values.FirstOrDefault(x => x.Name.Equals(role.Name, StringComparison.CurrentCultureIgnoreCase));
                if (e is not null && e.RoleId != role.RoleId)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorRoleExist, authService.I18n);
                }

                if (_roles.ContainsKey(role.RoleId))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);

                    var re = await db.FirstOrDefaultAsync(x => x.RecordId == role.RoleId && x.RecordType == IFrpRoleService.RecordTypeRole);
                    if (re is null)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
                    }

                    await db.DeleteAsync(re);
                    await db.AddAsync(
                        IFrpRecordContext.GetRecord(authService.User.UserId, role.RoleId, IFrpRoleService.RecordTypeRole, re.Owner, GrpcJson.GetJson(role)));

                    await db.SaveChangesAndCloseAsync();
                    await AddLogAsync(IFrpLogService.ActionChange, re, authService);

                    _roles[role.RoleId] = role;

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeRoleAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (string.IsNullOrWhiteSpace(role.RoleId))
                    return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);

                if (_roles.TryRemove(role.RoleId, out FrpRole? value))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);

                    var dbr = await db.FirstOrDefaultAsync(x => x.RecordId == role.RoleId && x.RecordType == IFrpRoleService.RecordTypeRole);
                    if (dbr is null)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
                    }

                    await db.DeleteAsync(dbr);

                    var uirs = _userInRoles.Where(x => x.Value.RoleId == role.RoleId).ToArray();
                    foreach (var item in uirs)
                    {
                        var res = await DeleteUserFromRoleAsync(item.Value, authService);
                        if (res.ErrorType is not FrpErrorType.ErrorNone)
                            return res;
                    }

                    //TODO delete access

                    await db.SaveChangesAndCloseAsync();
                    await AddLogAsync(IFrpLogService.ActionDelete, dbr, authService);

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteRoleAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ResetRoleAsync(FrpLog log, IFrpAuthService authService)
        {
            if (authService.IsAdmin == false)
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

            try
            {
                if (GrpcJson.TryGetModel<FrpRecord>(log.Record, out var r))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);

                    if (log.Action == IFrpLogService.ActionAdd || log.Action == IFrpLogService.ActionChange)
                    {
                        var old = await db.FirstOrDefaultAsync(x => x.RecordId == r.RecordId);
                        if (old is not null)
                        {
                            await db.DeleteAsync(old);
                        }
                    }

                    if (log.Action == IFrpLogService.ActionAdd)
                    {
                        _roles.TryRemove(r.RecordId, out _);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    else if (GrpcJson.TryGetModel<FrpRole>(r.DataAsJson, out var a))
                    {
                        _roles[r.RecordId] = a;
                        await db.AddAsync(r);
                        await db.SaveChangesAndCloseAsync();

                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    await db.CloseDatabaseAsync();
                }

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ResetRoleAsync), ex, authService);
                _log.LogError(ex, nameof(ResetRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> AddUserToRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin is false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                string id = userInRole.UserId + userInRole.RoleId;

                if (_userInRoles.ContainsKey(id))
                {
                    return FrpResponse.ErrorNone();
                }

                if (_roles.ContainsKey(userInRole.RoleId) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
                }

                if (await _ds.FrpUserService.IsUserByIdExistsAsync(userInRole.UserId) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
                }

                userInRole.UserInRoleId = id;
                if (_userInRoles.TryAdd(userInRole.UserInRoleId, userInRole))
                {
                    string json = GrpcJson.GetJson(userInRole);
                    var r = IFrpRecordContext.GetRecord(
                        authService.User.UserId, userInRole.UserInRoleId, IFrpRoleService.RecordTypeUserInRole, userInRole.RoleId, json);

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    await db.AddAsync(r);
                    await db.SaveChangesAndCloseAsync();

                    await AddLogAsync(IFrpLogService.ActionAdd, r, authService);

                    return FrpResponse.Create(json);
                }

                throw new Exception("Can not add user to role");
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddUserToRoleAsync), ex, authService);
                _log.LogError(ex, nameof(AddUserToRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteUserFromRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                userInRole.UserInRoleId = userInRole.UserId + userInRole.RoleId;

                if (_userInRoles.TryRemove(userInRole.UserInRoleId, out _))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var dbr = await db.FirstOrDefaultAsync(x => x.RecordType == IFrpRoleService.RecordTypeUserInRole && x.RecordId == userInRole.UserInRoleId);
                    if (dbr is not null)
                    {
                        await db.DeleteAsync(dbr);
                        await db.SaveChangesAndCloseAsync();
                        await AddLogAsync(IFrpLogService.ActionDelete, dbr, authService);
                    }

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorNotFound, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteUserFromRoleAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteUserFromRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ResetUserInRoleAsync(FrpLog log, IFrpAuthService authService)
        {
            if (authService.IsAdmin == false)
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

            try
            {
                if (GrpcJson.TryGetModel<FrpRecord>(log.Record, out var r))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    if (log.Action == IFrpLogService.ActionAdd)
                    {
                        var old = await db.FirstOrDefaultAsync(x => x.RecordId == r.RecordId);
                        if (old is not null)
                        {
                            await db.DeleteAsync(old);
                        }

                        _userInRoles.TryRemove(r.RecordId, out _);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    else if (GrpcJson.TryGetModel<FrpUserInRole>(r.DataAsJson, out var a))
                    {
                        _userInRoles[r.RecordId] = a;
                        await db.AddAsync(r);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                }

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ResetUserInRoleAsync), ex, authService);
                _log.LogError(ex, nameof(ResetUserInRoleAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

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

        private async ValueTask AddEventLogAsync(FrpEventLogLevel lvl, string location, string msg, string obj, IFrpAuthService authService)
        {
            await _logService.AddEventLogAsync(new()
            {
                Location = $"{nameof(FrpRoleService)}/{location}",
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                LogLevel = lvl,
                Message = msg,
                ObjectAsJson = obj,
            }, authService);
        }

        private async ValueTask AddExceptionAsync(string location, Exception ex, IFrpAuthService authService)
        {
            await _logService.AddExceptionLogAsync(new()
            {
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Location = $"{nameof(FrpRoleService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
