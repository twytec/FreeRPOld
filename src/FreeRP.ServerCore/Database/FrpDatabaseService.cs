using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Log;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FreeRP.ServerCore.Database
{
    public partial class FrpDatabaseService : IFrpDatabaseService
    {
        private readonly ConcurrentDictionary<string, FrpDatabase> _databases = [];

        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpDatabase _db;
        private readonly FrpSettings _frpSettings;
        private readonly ILogger _log;

        private readonly IFrpAuthService _systemUser;

        private static string GetRecordType(string databaseId, string tableId) => $"{databaseId}-{tableId}";


        public FrpDatabaseService(IFrpDataService ds, FrpSettings frpSettings, IFrpLogService logService, FrpDatabase db, ILogger logger)
        {
            _ds = ds;
            _logService = logService;
            _db = db;
            _frpSettings = frpSettings;
            _log = logger;

            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };
        }

        internal async Task InitAsync()
        {
            await LoadDatabases();
        }

        private async Task LoadDatabases()
        {
            try
            {
                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var dbs = await db.ListOrDefaultAsync(x => x.RecordType == IFrpDatabaseService.RecordTypeDatabase);
                if (dbs.Any())
                {
                    foreach (var r in dbs)
                    {
                        if (GrpcJson.GetModel<FrpDatabase>(r.DataAsJson) is FrpDatabase database)
                        {
                            _databases[r.RecordId] = database;
                        }
                    }
                }
                await db.CloseDatabaseAsync();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(LoadDatabases), ex, _systemUser);
                _log.LogError(ex, "Can not load databases");
            }
        }

        #region Get

        public ValueTask<IEnumerable<FrpDatabase>> GetAllDatabasesAsync() => ValueTask.FromResult(_databases.Values.AsEnumerable());

        public ValueTask<FrpDatabase?> GetDatabaseByIdAsync(string databaseId)
        {
            var data = _databases.TryGetValue(databaseId, out FrpDatabase? value) ? value : null;
            return ValueTask.FromResult(data);
        }

        #endregion

        #region Database

        public async ValueTask<FrpResponse> AddDatabaseAsync(FrpDatabase database, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin)
                {
                    var et = CheckDatabaseConfig(database);
                    if (et is not FrpErrorType.ErrorNone)
                        return FrpResponse.Create(et, authService.I18n);

                    if (_databases.ContainsKey(database.DatabaseId))
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorDatabaseExist, authService.I18n);
                    }

                    if (_databases.TryAdd(database.DatabaseId, database))
                    {
                        var r = IFrpRecordContext.GetRecord(
                                authService.User.UserId, database.DatabaseId,
                                IFrpDatabaseService.RecordTypeDatabase, authService.User.UserId, GrpcJson.GetJson(database));

                        var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                        await db.AddAsync(r);
                        await db.SaveChangesAndCloseAsync();

                        var context = new EfRecordContext(_frpSettings, database);
                        await context.Database.EnsureCreatedAsync();
                        await context.DisposeAsync();

                        await AddLogAsync(IFrpLogService.ActionAdd, r, authService);

                        return FrpResponse.ErrorNone();
                    }
                    else
                        throw new Exception("Can not add database");
                }

                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(AddDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin)
                {
                    var et = CheckDatabaseConfig(ndb);
                    if (et is not FrpErrorType.ErrorNone)
                        return FrpResponse.Create(et, authService.I18n);

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    if (
                        _databases.TryGetValue(ndb.DatabaseId, out FrpDatabase? odb) &&
                        await db.FirstOrDefaultAsync(x => x.RecordId == odb.DatabaseId && x.RecordType == IFrpDatabaseService.RecordTypeDatabase) is FrpRecord old)
                    {
                        await db.DeleteAsync(old);

                        await db.AddAsync(IFrpRecordContext.GetRecord(authService.User.UserId, ndb.DatabaseId, IFrpDatabaseService.RecordTypeDatabase, old.Owner, GrpcJson.GetJson(ndb)));
                        await db.SaveChangesAndCloseAsync();
                        _databases[ndb.DatabaseId] = ndb;

                        await AddLogAsync(IFrpLogService.ActionChange, old, authService);

                        if (_ds.FrpDatabaseAccessService is FrpDatabaseAccessService unit)
                            unit.DatabaseAccessRemoveDatabase(ndb.DatabaseId);

                        return FrpResponse.ErrorNone();
                    }
                    else
                    {
                        await db.CloseDatabaseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                    }
                }
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin)
                {
                    var et = CheckDatabaseConfig(ndb);
                    if (et is not FrpErrorType.ErrorNone)
                        return FrpResponse.Create(et, authService.I18n);

                    if (_databases.TryRemove(ndb.DatabaseId, out FrpDatabase? odb) && odb is not null)
                    {
                        var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                        var old = await db.FirstOrDefaultAsync(x => x.RecordId == odb.DatabaseId && x.RecordType == IFrpDatabaseService.RecordTypeDatabase);
                        if (old != null)
                        {
                            await db.DeleteAsync(old);
                            await db.SaveChangesAsync();

                            await AddLogAsync(IFrpLogService.ActionDelete, old, authService);
                        }
                        await db.CloseDatabaseAsync();

                        if (_ds.FrpDatabaseAccessService is FrpDatabaseAccessService unit)
                            unit.DatabaseAccessRemoveDatabase(ndb.DatabaseId);

                        return FrpResponse.ErrorNone();
                    }
                    else
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, authService.I18n);
                    }
                }
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ResetDatabaseAsync(FrpLog log, IFrpAuthService authService)
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
                        _databases.TryRemove(r.RecordId, out _);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    else if (GrpcJson.TryGetModel<FrpDatabase>(r.DataAsJson, out var odb))
                    {
                        _databases[r.RecordId] = odb;
                        await db.AddAsync(r);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }

                    if (_ds.FrpDatabaseAccessService is FrpDatabaseAccessService unit)
                        unit.DatabaseAccessRemoveDatabase(r.RecordId);
                }

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ResetDatabaseAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteDatabaseAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public static FrpErrorType CheckDatabaseConfig(FrpDatabase db)
        {
            if (string.IsNullOrWhiteSpace(db.DatabaseId))
                return FrpErrorType.ErrorDatabaseIdRequired;

            if (db.Datasets.Count > 0)
            {
                foreach (var t in db.Datasets)
                {
                    if (string.IsNullOrEmpty(t.DatasetId))
                        return FrpErrorType.ErrorDatasetIdRequired;

                    if (db.Datasets.Where(x => x.DatasetId == t.DatasetId).Count() > 1)
                        return FrpErrorType.ErrorDatasetExist;

                    if (t.Fields.Count > 0)
                    {
                        if (t.Fields.FirstOrDefault(x => x.IsPrimaryKey) is null)
                            return FrpErrorType.ErrorDatasetPrimaryKeyRequired;

                        foreach (var f in t.Fields)
                        {
                            if (f.IsPrimaryKey)
                                f.DataType = FrpDatabaseDataType.FieldString;

                            var et = CheckDataFieldConfig(f);
                            if (et is not FrpErrorType.ErrorNone)
                                return et;

                            if (t.Fields.Where(x => x.FieldId == f.FieldId).Count() > 1)
                                return FrpErrorType.ErrorFieldExist;
                        }
                    }
                }
            }

            return FrpErrorType.ErrorNone;
        }

        public static FrpErrorType CheckDataFieldConfig(FrpDataField f)
        {
            if (
                f.DataType == FrpDatabaseDataType.FieldNull ||
                f.DataType == FrpDatabaseDataType.FieldString ||
                f.DataType == FrpDatabaseDataType.FieldNumber ||
                f.DataType == FrpDatabaseDataType.FieldBoolean)
            {
                f.Fields.Clear();
            }

            f.FieldId = Json.ToCamelCase(f.FieldId);

            if (string.IsNullOrEmpty(f.FieldId))
                return FrpErrorType.ErrorFieldIdRequired;

            if (f.Fields.Count > 0)
            {
                foreach (var item in f.Fields)
                {
                    if (f.Fields.Where(x => x.FieldId == f.FieldId).Count() > 1)
                        return FrpErrorType.ErrorFieldExist;

                    var et = CheckDataFieldConfig(item);
                    if (et is not FrpErrorType.ErrorNone)
                        return et;
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
                Location = $"{nameof(FrpDatabaseService)}/{location}",
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
                Location = $"{nameof(FrpDatabaseService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
