using FreeRP.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Database
{
    public class FrpQueryable<T>(IFrpDatabaseAccess db) : IFrpQueryable<T>
    {
        private readonly IFrpDatabaseAccess _db = db;
        private readonly List<FrpQuery> _queries = [];
        public IEnumerable<FrpQuery> GetQueries => _queries;
        private readonly string _datasetName = typeof(T).Name;

        private int _skipe = 0;
        private int _take = 0;

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predExpr)
        {
            Where(predExpr);
            return await _db.FirstOrDefaultAsync(this);
        }

        public IFrpQueryable<T> Skip(int offset)
        {
            _skipe = offset;
            return this;
        }

        public IFrpQueryable<T> Take(int count)
        {
            _take = count;
            return this;
        }

        public async Task<IEnumerable<T>> ToArrayAsync()
        {
            return (await ToListAsync()).ToArray();
        }

        public async Task<IEnumerable<T>> ToListAsync()
        {
            var qr = new FrpQueryRequest()
            {
                DatabaseId = _db.FrpDatabaseId,
                DatasetId = _datasetName,
                Skipe = _skipe,
                Take = _take
            };
            qr.Queries.AddRange(_queries);

            var res = await _db.QueryAsync(qr);
            if (res is not null)
            {
                if (res.ErrorType == FrpErrorType.ErrorNone)
                {
                    var list = Json.GetModel<List<T>>(res.Data);
                    if (list is not null)
                        return list;

                    return [];
                }

                FrpException.Error(res.ErrorType, res.Message);
            }

            return [];
        }

        public IFrpQueryable<T> Where(Expression<Func<T, bool>> predExpr)
        {
            var q = new Helpers.Database.FrpLinqExpressionVisitor(predExpr).Resolve();
            if (_queries.Count != 0 && q.Count != 0)
            {
                _queries.Last().Next = FrpQueryType.QueryAndAlso;
                _queries.AddRange(q);
            }
            else
            {
                _queries.AddRange(q);
            }

            return this;
        }
    }
}
