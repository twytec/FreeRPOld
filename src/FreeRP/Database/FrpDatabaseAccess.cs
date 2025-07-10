using FreeRP.FrpServices;
using FreeRP.Helpers;
using System.Data;
using System.Linq.Expressions;

namespace FreeRP.Database
{
    public class FrpDatabaseAccess(string databaseId, IFrpDataService ds, IFrpAuthService auth) : IFrpDatabaseAccess
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        public string FrpDatabaseId { get; set; } = databaseId;

        #region Open, save and close

        public async ValueTask<bool> OpenDatabaseAsync()
        {
            var res = await _ds.FrpDatabaseAccessService.OpenDatabaseAsync(FrpDatabaseId, _auth);
            if (res.ErrorType is not FrpErrorType.ErrorNone)
                FrpException.Error(res.ErrorType, res.Message);
            else
                return true;

            return false;
        }

        public async ValueTask<bool> SaveChangesAsync()
        {
            var res = await _ds.FrpDatabaseAccessService.SaveChangesAsync(FrpDatabaseId, _auth);
            if (res.ErrorType is not FrpErrorType.ErrorNone)
                FrpException.Error(res.ErrorType, res.Message);
            else
                return true;

            return false;
        }

        public async ValueTask<bool> CloseDatabaseAsync()
        {
            var res = await _ds.FrpDatabaseAccessService.CloseDatabaseAsync(FrpDatabaseId, _auth);
            if (res.ErrorType is not FrpErrorType.ErrorNone)
                FrpException.Error(res.ErrorType, res.Message);
            else
                return true;

            return false;
        }

        #endregion

        #region Add

        public async ValueTask<T> AddAsync<T>(T item)
        {
            if (item == null)
                throw new NoNullAllowedException(nameof(item));

            var r = new FrpDataRequest()
            {
                DatabaseId = FrpDatabaseId,
                DatasetId = typeof(T).Name
            };

            var json = Json.GetJson(item);
            r.Data = Google.Protobuf.ByteString.CopyFromUtf8(json);

            FrpResponse res = await _ds.FrpDatabaseAccessService.AddDatasetAsync(r, _auth);
            if (res.ErrorType == FrpErrorType.ErrorNone)
            {
                var m = Json.GetModel<T>(res.Data);
                if (m is not null)
                    return m;
            }
            else
                throw FrpException.GetFrpException(res.ErrorType, res.Message);

            FrpException.Error(FrpErrorType.ErrorUnknown, "");
            return default!;
        }

        public async ValueTask<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> items)
        {
            List<T> list = [];
            foreach (var item in items)
            {
                if (item == null)
                    throw new NoNullAllowedException(nameof(item));

                var m = await AddAsync(item);
                list.Add(m);
            }

            return list;
        }

        #endregion

        #region Change

        public async ValueTask ChangeAsync<T>(T item)
        {
            if (item == null)
                throw new NoNullAllowedException(nameof(item));

            var idProp = item.GetType()
                .GetProperties()
                .FirstOrDefault(x => x.Name.Equals(IFrpDatabaseService.DatabasePrimaryKeyName, StringComparison.CurrentCultureIgnoreCase));

            if (idProp is null)
            {
                throw new MissingPrimaryKeyException(nameof(item));
            }

            if (idProp.GetValue(item) is not string id)
            {
                throw new MissingPrimaryKeyException(nameof(item));
            }

            var r = new FrpDataRequest()
            {
                DatabaseId = FrpDatabaseId,
                DatasetId = typeof(T).Name,
                DataId = id,
            };

            var json = Json.GetJson(item);
            r.Data = Google.Protobuf.ByteString.CopyFromUtf8(json);

            FrpResponse res = await _ds.FrpDatabaseAccessService.ChangeDatasetAsync(r, _auth);
            if (res.ErrorType is FrpErrorType.ErrorNone)
                return;
            else
                throw FrpException.GetFrpException(res.ErrorType, res.Message);
        }

        public async ValueTask ChangeRangeAsync<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (item == null)
                    throw new NoNullAllowedException(nameof(item));

                await ChangeAsync(item);
            }
        }

        #endregion

        #region Delete

        public async ValueTask DeleteAsync<T>(string id)
        {
            if (id == null)
                throw new NoNullAllowedException(nameof(id));

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            var r = new FrpDataRequest()
            {
                DatabaseId = FrpDatabaseId,
                DatasetId = typeof(T).Name,
                DataId = id
            };

            FrpResponse res = await _ds.FrpDatabaseAccessService.DeleteDatasetAsync(r, _auth);

            if (res.ErrorType is not FrpErrorType.ErrorNone)
            {
                throw FrpException.GetFrpException(res.ErrorType, res.Message);
            }
        }

        public async ValueTask DeleteRangeAsync<T>(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                await DeleteAsync<T>(id);
            }
        }

        #endregion

        #region FirstOrDefaultAsync

        public async ValueTask<T?> FirstOrDefaultAsync<T>(IFrpQueryable<T> q)
        {
            FrpQueryRequest qr = new()
            {
                DatabaseId = FrpDatabaseId,
                DatasetId = typeof(T).Name,
            };
            qr.Queries.AddRange(q.GetQueries);

            FrpResponse res = await _ds.FrpDatabaseAccessService.FirstOrDefaultAsync(qr, _auth);

            if (res.ErrorType == FrpErrorType.ErrorNone)
            {
                var m = Json.GetModel<T>(res.Data);
                if (m is not null)
                    return m;
            }
            else
                throw FrpException.GetFrpException(res.ErrorType, res.Message);

            FrpException.Error(FrpErrorType.ErrorUnknown, "");
            return default;
        }

        public async ValueTask<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predExpr)
        {
            if (predExpr is null)
                throw new NoNullAllowedException(nameof(predExpr));

            var q = Where(predExpr);
            return await FirstOrDefaultAsync(q);
        }

        #endregion

        public async ValueTask<FrpResponse?> QueryAsync(FrpQueryRequest qr)
        {
            return await _ds.FrpDatabaseAccessService.ListOrDefaultAsync(qr, _auth);
        }

        public IFrpQueryable<T> Where<T>(Expression<Func<T, bool>> predExpr)
        {
            return new FrpQueryable<T>(this).Where(predExpr);
        }
    }
}
