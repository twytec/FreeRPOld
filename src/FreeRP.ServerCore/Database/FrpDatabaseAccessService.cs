using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Helpers.Database;
using FreeRP.Log;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace FreeRP.ServerCore.Database
{
    public class FrpDatabaseAccessService : IFrpDatabaseAccessService
    {
        private record OpenDatabase(IFrpRecordContext Context, FrpDatabase Database, string UserId);
        private readonly ConcurrentDictionary<string, OpenDatabase> _openDatabases = [];
        private readonly ConcurrentDictionary<string, FrpDatabasePermissions> _databasesAccess = [];

        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpSettings _frpSettings;
        private readonly ILogger _log;
        private readonly IFrpAuthService _systemUser;

        public static string GetRecordType(string databaseId, string datasetId) => $"{databaseId}-{datasetId}";
        private static string GetUserDatabaseKey(string userId, string databaseId) => $"{userId}{databaseId}";

        public FrpDatabaseAccessService(IFrpDataService ds, IFrpLogService logService, FrpSettings frpSettings, ILogger logger)
        {
            _ds = ds;
            _logService = logService;
            _frpSettings = frpSettings;
            _log = logger;

            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };
        }

        #region Access

        internal void DatabaseAccessRemoveDatabase(string databaseId)
        {
            var acs = _databasesAccess.Where(x => x.Key.EndsWith(databaseId));
            if (acs.Any())
            {
                foreach (var ac in acs)
                {
                    _databasesAccess.TryRemove(ac);
                }
            }
        }

        public async ValueTask<FrpDatabasePermissions> GetDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService)
        {
            var key = GetUserDatabaseKey(authService.User.UserId, db.DatabaseId);
            if (_databasesAccess.TryGetValue(key, out var dbUserAccess))
            {
                return dbUserAccess;
            }

            List<FrpPermission> access;

            if (db.AccessMode == FrpAccessMode.AccessModeCustom)
            {
                access = [
                    new FrpPermission() {
                        AccessUri = $"{IFrpDatabaseService.UriSchemeDatabase}://{db.DatabaseId}",
                        PermissionValues = new() {
                            Add = FrpPermissionValue.Allow,
                            Change = FrpPermissionValue.Allow,
                            Delete = FrpPermissionValue.Allow,
                            Read = FrpPermissionValue.Allow,
                        }
                    }
                ];
            }
            else
            {
                access = new(await _ds.FrpPermissionService.GetUserDatabasePermissionsAsync(db, authService));
            }

            FrpDatabasePermissions da = new(db);
            da.SetPermission(access);
            _databasesAccess[key] = da;

            return da;
        }

        #endregion

        public async ValueTask<FrpResponse> OpenDatabaseAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            try
            {
                var database = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(frpDatabaseId);
                if (database is null)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                }

                var db = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(database.DatabaseId);
                if (db is null)
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);

                string key = GetUserDatabaseKey(authService.User.UserId, db.DatabaseId);
                OpenDatabase odb = new(FrpRecordContextFactory.Create(_frpSettings, db), db, authService.User.UserId);

                var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                if (FrpPermissionValues.IsAny(ac.Main.PermissionValues, FrpPermissionValue.Allow) == false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                }

                if (_openDatabases.ContainsKey(key) is false)
                    _openDatabases[key] = odb;

                await AddEventLogAsync(FrpEventLogLevel.Information, nameof(OpenDatabaseAsync), "Open database", odb.Database.DatabaseId, authService);

                return FrpResponse.ErrorNone();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(OpenDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(OpenDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> SaveChangesAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            try
            {
                var database = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(frpDatabaseId);
                if (database is null)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                }

                var key = GetUserDatabaseKey(authService.User.UserId, database.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var db))
                {
                    await db.Context.SaveChangesAsync();
                    await AddEventLogAsync(FrpEventLogLevel.Information, nameof(SaveChangesAsync), "Save database", db.Database.DatabaseId, authService);

                    return FrpResponse.ErrorNone();
                }
                return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(SaveChangesAsync), ex, authService);
                _log.LogError(ex, nameof(SaveChangesAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> CloseDatabaseAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            try
            {
                var database = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(frpDatabaseId);
                if (database is null)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                }

                var key = GetUserDatabaseKey(authService.User.UserId, database.DatabaseId);
                if (_openDatabases.TryRemove(key, out var db))
                {
                    await db.Context.CloseDatabaseAsync();
                    await AddEventLogAsync(FrpEventLogLevel.Information, nameof(CloseDatabaseAsync), "Close database", db.Database.DatabaseId, authService);

                    return FrpResponse.ErrorNone();
                }
                return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(CloseDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(CloseDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> AddDatasetAsync(FrpDataRequest req, IFrpAuthService authService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.DatabaseId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIdRequired, authService.I18n);
                }

                if (string.IsNullOrWhiteSpace(req.DatasetId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetIdRequired, authService.I18n);
                }

                string key = GetUserDatabaseKey(authService.User.UserId, req.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var odb))
                {
                    var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                    if (ac.Main.PermissionValues.Add is not FrpPermissionValue.Allow)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    var dataset = odb.Database.Datasets.FirstOrDefault(x => x.DatasetId == req.DatasetId);

                    #region if dataset is null - add new data struct if allowed

                    if (dataset is null)
                    {
                        if (odb.Database.AllowUnknownData)
                        {
                            try
                            {
                                dataset = await FrpDatasetFromJson.GetDatasetAsync(req.Data.Memory.Span, req.DatasetId);
                            }
                            catch (Exception ex)
                            {
                                if (ex is FrpException fex)
                                    return FrpResponse.Create(fex.ErrorType, authService.I18n);

                                await AddExceptionAsync(
                                    $"{nameof(FrpDatabaseService)}/{nameof(AddDatasetAsync)}/{nameof(FrpDatasetFromJson.GetDatasetAsync)}", ex, authService);
                                _log.LogError(ex, nameof(AddDatasetAsync));

                                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
                            }

                            odb.Database.Datasets.Add(dataset);
                            var res = await _ds.FrpDatabaseService.ChangeDatabaseAsync(odb.Database, _systemUser);
                            if (res.ErrorType is not FrpErrorType.ErrorNone)
                                return res;

                            ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                        }
                        else
                        {
                            return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotAllowedUnkownData, authService.I18n);
                        }
                    }

                    #endregion

                    string id = FrpId.NewId();
                    string? json = null;

                    try
                    {
                        json = await FrpProcessJsonToAdd.GetJsonAsync(odb.Database, dataset, ac, id, req.Data.Memory.Span);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FrpException fex && fex.ErrorType == FrpErrorType.ErrorDatasetDataInvalid)
                        {
                            await FrpDatasetUpdateFromJson.UpdateAsync(req.Data.Memory.Span, dataset);
                            var res = await _ds.FrpDatabaseService.ChangeDatabaseAsync(odb.Database, _systemUser);
                            if (res.ErrorType is not FrpErrorType.ErrorNone)
                                return res;

                            ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                            json = await FrpProcessJsonToAdd.GetJsonAsync(odb.Database, dataset, ac, id, req.Data.Memory.Span);
                        }
                        else
                        {
                            await AddExceptionAsync($"{nameof(AddDatasetAsync)}/{nameof(FrpProcessJsonToAdd.GetJson)}", ex, authService);
                            _log.LogError(ex, nameof(AddDatasetAsync));

                            return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
                        }
                    }

                    if (json is null)
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                    var r = IFrpRecordContext.GetRecord(authService.User.UserId, id, GetRecordType(req.DatabaseId, req.DatasetId), odb.Database.Owner, json);
                    await odb.Context.AddAsync(r);

                    await AddLogAsync(IFrpLogService.ActionAdd, r, authService, req.DatabaseId);

                    return FrpResponse.Create(json);
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddDatasetAsync), ex, authService);
                _log.LogError(ex, nameof(AddDatasetAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dataRequest.DatabaseId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIdRequired, authService.I18n);
                }

                if (string.IsNullOrWhiteSpace(dataRequest.DatasetId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetIdRequired, authService.I18n);
                }

                if (string.IsNullOrWhiteSpace(dataRequest.DataId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetPrimaryKeyRequired, authService.I18n);
                }

                string key = GetUserDatabaseKey(authService.User.UserId, dataRequest.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var odb))
                {
                    var dataset = odb.Database.Datasets.FirstOrDefault(x => x.DatasetId == dataRequest.DatasetId);
                    if (dataset is null)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorDatasetNotExist, authService.I18n);
                    }

                    var old = await odb.Context.FirstOrDefaultAsync(x => x.RecordId == dataRequest.DataId);
                    if (old is null)
                        return FrpResponse.Create(FrpErrorType.ErrorNotFound, authService.I18n);

                    var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                    if (ac.Main.PermissionValues.Change is not FrpPermissionValue.Allow)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    string? json = null;

                    try
                    {
                        json = await FrpProcessJsonToChange
                            .GetJsonAsync(odb.Database, dataset, ac, old.RecordId, dataRequest.Data.Memory.Span, old.DataAsJson);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FrpException fex && fex.ErrorType == FrpErrorType.ErrorDatasetDataInvalid)
                        {
                            await FrpDatasetUpdateFromJson.UpdateAsync(dataRequest.Data.Memory.Span, dataset);
                            var res = await _ds.FrpDatabaseService.ChangeDatabaseAsync(odb.Database, _systemUser);
                            if (res.ErrorType is not FrpErrorType.ErrorNone)
                                return res;

                            ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                            json = await FrpProcessJsonToChange
                                .GetJsonAsync(odb.Database, dataset, ac, old.RecordId, dataRequest.Data.Memory.Span, old.DataAsJson);
                        }
                        else
                        {
                            await AddExceptionAsync($"{nameof(AddDatasetAsync)}/{nameof(FrpProcessJsonToAdd.GetJson)}", ex, authService);
                            _log.LogError(ex, nameof(AddDatasetAsync));

                            return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
                        }
                    }

                    if (json is null)
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                    await odb.Context.DeleteAsync(old);
                    await odb.Context.AddAsync(IFrpRecordContext.GetRecord(authService.User.UserId, old.RecordId, old.RecordType, old.Owner, json));

                    await AddLogAsync(IFrpLogService.ActionChange, old, authService, dataRequest.DatabaseId);

                    return FrpResponse.ErrorNone();
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeDatasetAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeDatasetAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dataRequest.DatabaseId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIdRequired, authService.I18n);
                }

                if (string.IsNullOrWhiteSpace(dataRequest.DatasetId))
                {
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetIdRequired, authService.I18n);
                }

                string key = GetUserDatabaseKey(authService.User.UserId, dataRequest.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var odb))
                {
                    var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                    if (ac.Main.PermissionValues.Delete is not FrpPermissionValue.Allow)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    var table = odb.Database.Datasets.FirstOrDefault(x => x.DatasetId == dataRequest.DatasetId);

                    if (table is null)
                        return FrpResponse.Create(FrpErrorType.ErrorDatasetNotExist, authService.I18n);

                    if (ac.GetDatasetPermission(table.DatasetId) is FrpPermissionValues pv)
                    {
                        if (pv.Delete is not FrpPermissionValue.Allow)
                            return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }
                    else
                        return FrpResponse.Create(FrpErrorType.ErrorDatasetNotExist, authService.I18n);

                    var old = await odb.Context
                        .FirstOrDefaultAsync(x => x.RecordType == GetRecordType(dataRequest.DatabaseId, dataRequest.DatasetId) && x.RecordId == dataRequest.DataId);

                    if (old is null)
                        return FrpResponse.Create(FrpErrorType.ErrorNotFound, authService.I18n);

                    await odb.Context.DeleteAsync(old);
                    await AddLogAsync(IFrpLogService.ActionDelete, old, authService, dataRequest.DatabaseId);

                    return FrpResponse.ErrorNone();
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteDatasetAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteDatasetAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> FirstOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queryRequest.DatabaseId))
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIdRequired, authService.I18n);

                if (string.IsNullOrWhiteSpace(queryRequest.DatasetId))
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetIdRequired, authService.I18n);

                string key = GetUserDatabaseKey(authService.User.UserId, queryRequest.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var odb))
                {
                    var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                    if (ac.Main.PermissionValues.Read is not FrpPermissionValue.Allow)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    var dataset = odb.Database.Datasets.FirstOrDefault(x => x.DatasetId == queryRequest.DatasetId);
                    if (dataset is null)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorDatasetNotExist, authService.I18n);
                    }

                    var data = await odb.Context.FirstOrDefaultAsync(queryRequest);
                    if (data is not null)
                    {
                        if (ac.AllReadAllowed)
                            return FrpResponse.Create(data.DataAsJson);

                        var json = await FrpProcessJsonToRead.GetJsonAsync(odb.Database, dataset, ac, data.DataAsJson);

                        if (json is not null)
                            return FrpResponse.Create(json);
                        else
                            return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    return FrpResponse.Create(FrpErrorType.ErrorNotFound, authService.I18n);
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);


            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(FirstOrDefaultAsync), ex, authService);
                _log.LogError(ex, nameof(FirstOrDefaultAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ListOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(queryRequest.DatabaseId))
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIdRequired, authService.I18n);

                if (string.IsNullOrWhiteSpace(queryRequest.DatasetId))
                    return FrpResponse.Create(FrpErrorType.ErrorDatasetIdRequired, authService.I18n);

                string key = GetUserDatabaseKey(authService.User.UserId, queryRequest.DatabaseId);
                if (_openDatabases.TryGetValue(key, out var odb))
                {
                    var ac = await GetDatabasePermissionsAsync(odb.Database, authService);
                    if (ac.Main.PermissionValues.Read is not FrpPermissionValue.Allow)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                    }

                    var dataset = odb.Database.Datasets.FirstOrDefault(x => x.DatasetId == queryRequest.DatasetId);
                    if (dataset is null)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorNotFound, authService.I18n);
                    }

                    var datas = await odb.Context.ListOrDefaultAsync(queryRequest);

                    if (datas is not null && datas.Any())
                    {
                        StringBuilder sb = new();
                        sb.Append('[');

                        foreach (var d in datas)
                        {
                            if (ac.AllReadAllowed)
                            {
                                sb.Append($"{d.DataAsJson},");
                            }
                            else
                            {
                                var json = await FrpProcessJsonToRead.GetJsonAsync(odb.Database, dataset, ac, d.DataAsJson);
                                if (json is not null)
                                    sb.Append($"{json},");
                            }
                        }

                        if (sb[^1] == ',')
                            sb.Length--;

                        sb.Append(']');
                        return FrpResponse.Create(sb.ToString());
                    }
                    else
                        return FrpResponse.Create("[]");
                }
                else
                    return FrpResponse.Create(FrpErrorType.ErrorDatabaseIsNotOpen, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ListOrDefaultAsync), ex, authService);
                _log.LogError(ex, nameof(ListOrDefaultAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ResetDatabaseAccessAsync(FrpLog log, IFrpAuthService authService)
        {
            if (authService.IsAdmin == false)
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

            try
            {
                var frpDb = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(log.Val1);
                if (frpDb is not null)
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, frpDb);

                    if (GrpcJson.TryGetModel<FrpRecord>(log.Record, out var r))
                    {
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
                            await db.SaveChangesAsync();
                            await db.CloseDatabaseAsync();
                            return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                        }
                        else
                        {
                            await db.AddAsync(r);
                            await db.SaveChangesAsync();
                            await db.CloseDatabaseAsync();
                            return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                        }
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ResetDatabaseAccessAsync), ex, authService);
                _log.LogError(ex, nameof(ResetDatabaseAccessAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        private async ValueTask AddLogAsync(string action, FrpRecord old, IFrpAuthService authService, string databaseId, string val1 = "", string val2 = "", string val3 = "", string val4 = "", string val5 = "")
        {
            await _logService.AddLogAsync(new()
            {
                Action = action,
                UtcUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RecordId = old.RecordId,
                Record = Json.GetJson(old),
                RecordType = old.RecordType,
                DatabaseId = databaseId,
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
                Location = $"{nameof(FrpDatabaseAccessService)}/{location}",
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
                Location = $"{nameof(FrpDatabaseAccessService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}