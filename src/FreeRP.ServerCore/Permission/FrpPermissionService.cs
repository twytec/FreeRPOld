using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Log;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Database;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FreeRP.ServerCore.Permission
{
    public class FrpPermissionService : IFrpPermissionService
    {
        private readonly ConcurrentDictionary<string, FrpPermission> _permissions = [];

        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpDatabase _db;
        private readonly ILogger _log;
        private readonly FrpSettings _frpSettings;
        private readonly IFrpAuthService _systemUser;

        public FrpPermissionService(IFrpDataService ds, FrpSettings frpSettings, IFrpLogService logService, FrpDatabase db, ILogger logger)
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
            await LoadPermission();
        }

        private async Task LoadPermission()
        {
            try
            {
                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var das = await db.ListOrDefaultAsync(x => x.RecordType == IFrpPermissionService.RecordTypePermission);
                if (das.Any())
                {
                    foreach (var da in das)
                    {
                        if (GrpcJson.GetModel<FrpPermission>(da.DataAsJson) is FrpPermission data)
                        {
                            _permissions[data.MemberIdAccessUri] = data;
                        }
                    }
                }
                await db.CloseDatabaseAsync();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(LoadPermission), ex, _systemUser);
                _log.LogError(ex, nameof(LoadPermission));
            }
        }

        #region Get

        public ValueTask<FrpPermission?> GetPermissionByIdAsync(string id)
        {
            var data = _permissions.TryGetValue(id, out FrpPermission? value) ? value : null;
            return ValueTask.FromResult(data);
        }
        public ValueTask<IEnumerable<FrpPermission>> GetDatabasePermissionsAsync(string databaseId)
        {
            var data = _permissions.Values.Where(
                x => x.AccessUri.StartsWith($"{IFrpDatabaseService.UriSchemeDatabase}://{databaseId}")).OrderBy(x => x.AccessUri)
                .AsEnumerable();
            return ValueTask.FromResult(data);
        }

        public ValueTask<IEnumerable<FrpPermission>> GetContentPermissionsAsync(string uri)
        {
            var data = _permissions.Values.Where(x => x.AccessUri == uri);
            return ValueTask.FromResult(data);
        }

        #endregion

        #region Add, change, delete, reset

        public async ValueTask<FrpResponse> AddPermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                ac.PermissionValues ??= new();

                if (ac.MemberIdKind is MemberIdKind.Role && await _ds.FrpRoleService.IsRoleByIdExistsAsync(ac.MemberId) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, authService.I18n);
                }
                else if (ac.MemberIdKind is MemberIdKind.User && await _ds.FrpUserService.IsUserByIdExistsAsync(ac.MemberId) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
                }

                if (FrpUri.TryCreate(ac.AccessUri, out var uri))
                {
                    ac.AccessUri = uri.GetUriAsString();
                    ac.SetMemberIdAccessUri();

                    if (_permissions.ContainsKey(ac.MemberIdAccessUri))
                        return FrpResponse.Create(FrpErrorType.ErrorAccessExist, authService.I18n);

                    if (uri.Scheme is FrpUriScheme.File)
                    {
                        ac.AccessUriScheme = AccessUriScheme.Content;

                        if (Path.Exists(uri.ToAbsolutPath(_frpSettings.ContentSettings.ContentRootPath)) == false)
                            return FrpResponse.Create(FrpErrorType.ErrorPathNotExist, authService.I18n);
                    }
                    else if (uri.Scheme is FrpUriScheme.Database)
                    {
                        ac.AccessUriScheme = AccessUriScheme.Database;

                        if (await _ds.FrpDatabaseService.GetDatabaseByIdAsync(uri.Host) is FrpDatabase odb)
                        {
                            var et = CheckDatabasePermission(ac, odb, authService);
                            if (et is not FrpErrorType.ErrorNone)
                                return FrpResponse.Create(et, authService.I18n);
                        }
                        else
                            return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                    }
                    else if (uri.Scheme == FrpUriScheme.Plugin)
                    {
                        throw new NotImplementedException();
                    }
                    else
                        return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = IFrpRecordContext.GetRecord(authService.User.UserId, ac.MemberIdAccessUri, IFrpPermissionService.RecordTypePermission, authService.User.UserId, GrpcJson.GetJson(ac));
                    await db.AddAsync(r);
                    await db.SaveChangesAndCloseAsync();

                    await AddLogAsync(IFrpLogService.ActionAdd, r, authService);

                    _permissions[ac.MemberIdAccessUri] = ac;
                    return FrpResponse.Create(ac);
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorUriSchemeNotSupported, authService.I18n);
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(AddPermissionAsync), ex, authService);

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        public async ValueTask<FrpResponse> ChangePermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                ac.PermissionValues ??= new();

                if (_permissions.TryGetValue(ac.MemberIdAccessUri, out var da))
                {
                    ac.AccessUri = da.AccessUri;
                    ac.MemberId = da.MemberId;
                    ac.AccessUriScheme = da.AccessUriScheme;

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var old = await db.FirstOrDefaultAsync(x => x.RecordId == da.MemberIdAccessUri && x.RecordType == IFrpPermissionService.RecordTypePermission);
                    if (old is not null)
                    {
                        await db.DeleteAsync(old);

                        var r = IFrpRecordContext.GetRecord(authService.User.UserId, ac.MemberIdAccessUri, IFrpPermissionService.RecordTypePermission, old.Owner, GrpcJson.GetJson(ac));
                        await db.AddAsync(r);

                        await AddLogAsync(IFrpLogService.ActionChange, r, authService);
                        await db.SaveChangesAndCloseAsync();

                        _permissions[ac.MemberIdAccessUri] = ac;
                        return FrpResponse.ErrorNone();
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(ChangePermissionAsync), ex, authService);

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        public async ValueTask<FrpResponse> DeletePermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_permissions.TryRemove(ac.MemberIdAccessUri, out var da))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var old = await db.FirstOrDefaultAsync(x => x.RecordId == ac.MemberIdAccessUri && x.RecordType == IFrpPermissionService.RecordTypePermission);
                    if (old is not null)
                    {
                        await db.DeleteAsync(old);
                        await AddLogAsync(IFrpLogService.ActionDelete, old, authService);
                        await db.SaveChangesAndCloseAsync();

                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(DeletePermissionAsync), ex, authService);

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        public async ValueTask<FrpResponse> ResetPermissionAysnc(FrpLog log, IFrpAuthService authService)
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
                            await db.DeleteAsync(old);
                    }

                    if (log.Action == IFrpLogService.ActionAdd)
                    {
                        _permissions.TryRemove(r.RecordId, out _);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    else if (GrpcJson.TryGetModel<FrpPermission>(r.DataAsJson, out var a))
                    {
                        _permissions[r.RecordId] = a;
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
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(ResetPermissionAysnc), ex, authService);

                return FrpResponse.ErrorUnknown(ex.Message);
            }
        }

        #endregion

        #region Get user access

        #region Content

        public async ValueTask<FrpPermission> GetUserContentPermissionAsync(FrpUri uri, IFrpAuthService authService)
        {
            try
            {
                FrpPermission access = new() { PermissionValues = new() };

                var ac = await GetPermissionAsync(authService.User.UserId, uri);
                if (ac is not null && MergePermission(access.PermissionValues, ac.PermissionValues))
                    return access;

                foreach (var r in authService.Roles)
                {
                    ac = await GetPermissionAsync(r.RoleId, uri);
                    if (ac is not null && MergePermission(access.PermissionValues, ac.PermissionValues))
                        return access;
                }

                return access;
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(GetUserContentPermissionAsync), ex, authService);
                throw;
            }
        }

        private async ValueTask<FrpPermission?> GetPermissionAsync(string id, FrpUri uri)
        {
            try
            {
                string baseUri = $"{id}{uri.SchemeToString()}";

                var segList = uri.Segments.ToList();
                for (int i = segList.Count; i > 0; i--)
                {
                    string path = string.Join("/", segList);
                    string key = $"{baseUri}/{path}";
                    if (baseUri.EndsWith('/'))
                        key = $"{baseUri}{path}";

                    if (_permissions.TryGetValue(key, out var ac))
                        return ac;

                    segList.RemoveAt(i - 1);
                }

                if (_permissions.TryGetValue(baseUri, out var a))
                    return a;
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(GetPermissionAsync), ex, _systemUser);
            }

            return null;
        }

        #endregion

        #region Database

        public async ValueTask<IEnumerable<FrpPermission>> GetUserDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService)
        {
            List<FrpPermission> list = [];
            try
            {
                
                string dbPath = $"{IFrpDatabaseService.UriSchemeDatabase}://{db.DatabaseId}";

                if (db.AccessMode is FrpAccessMode.AccessModeUser)
                    list.AddRange(_permissions.Where(x => x.Key.StartsWith($"{authService.User.UserId}{dbPath}")).Select(x => x.Value));
                else if (db.AccessMode is FrpAccessMode.AccessModeRole)
                {
                    foreach (var r in authService.Roles)
                    {
                        var acs = _permissions.Where(x => x.Key.StartsWith($"{r.RoleId}{dbPath}")).Select(x => x.Value);
                        if (acs.Any())
                        {
                            db.Owner = r.RoleId;
                            list.AddRange(acs);
                            break;
                        }
                    }
                }

                return list.AsEnumerable();
            }
            catch (Exception ex)
            {
                _log.LogError(exception: ex, message: "");
                await AddExceptionAsync(nameof(GetUserDatabasePermissionsAsync), ex, authService);
            }

            return list.AsEnumerable();
        }

        #endregion

        private static bool MergePermission(FrpPermissionValues? a, FrpPermissionValues? b)
        {
            if (a is null || b is null)
                return false;

            if (a.Change is FrpPermissionValue.Undefined && b.Change is not FrpPermissionValue.Undefined)
                a.Change = b.Change;
            else if (a.Change is FrpPermissionValue.Denied && b.Change is FrpPermissionValue.Allow)
                a.Change = b.Change;

            if (a.Add is FrpPermissionValue.Undefined && b.Add is not FrpPermissionValue.Undefined)
                a.Add = b.Add;
            else if (a.Add is FrpPermissionValue.Denied && b.Add is FrpPermissionValue.Allow)
                a.Add = b.Add;

            if (a.Delete is FrpPermissionValue.Undefined && b.Delete is not FrpPermissionValue.Undefined)
                a.Delete = b.Delete;
            else if (a.Delete is FrpPermissionValue.Denied && b.Delete is FrpPermissionValue.Allow)
                a.Delete = b.Delete;

            if (a.Read is FrpPermissionValue.Undefined && b.Read is not FrpPermissionValue.Undefined)
                a.Read = b.Read;
            else if (a.Read is FrpPermissionValue.Denied && b.Read is FrpPermissionValue.Allow)
                a.Read = b.Read;

            if (FrpPermissionValues.AreAll(a, FrpPermissionValue.Allow))
                return true;

            return false;
        }

        #endregion

        #region Helpers

        private FrpErrorType CheckDatabasePermission(FrpPermission ac, FrpDatabase db, IFrpAuthService authService)
        {
            if (db.AccessMode is FrpAccessMode.AccessModeRole && ac.MemberIdKind is not MemberIdKind.Role)
                return FrpErrorType.ErrorDatabaseOnlyRoleAccess;
            else if (db.AccessMode is FrpAccessMode.AccessModeUser && ac.MemberIdKind is not MemberIdKind.User)
                return FrpErrorType.ErrorDatabaseOnlyUserAccess;

            if (ac.MemberIdKind is not MemberIdKind.User)
            {
                var acs = _permissions.Where(x => x.Key.Contains(db.DatabaseId)).Select(x => x.Value).ToList();
                foreach (var item in acs)
                {
                    if (item.MemberId != ac.MemberId)
                    {
                        if (item.MemberIdKind is MemberIdKind.Role)
                        {
                            if (authService.Roles.FirstOrDefault(x => x.RoleId == item.MemberId) is not null)
                                return FrpErrorType.ErrorAccessConflict;
                        }
                    }
                }
            }

            return FrpErrorType.ErrorNone;
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

        private async ValueTask AddEventLogAsync(FrpEventLogLevel lvl, string location, string msg, string obj, IFrpAuthService authService)
        {
            await _logService.AddEventLogAsync(new()
            {
                Location = $"{nameof(FrpPermissionService)}/{location}",
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
                Location = $"{nameof(FrpPermissionService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
