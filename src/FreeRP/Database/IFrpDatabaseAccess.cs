using System.Linq.Expressions;

namespace FreeRP.Database
{
    public interface IFrpDatabaseAccess
    {
        string FrpDatabaseId { get; set; }
        ValueTask<bool> OpenDatabaseAsync();
        ValueTask<bool> SaveChangesAsync();
        ValueTask<bool> CloseDatabaseAsync();

        ValueTask<T> AddAsync<T>(T item);
        ValueTask<IEnumerable<T>> AddRangeAsync<T>(IEnumerable<T> items);

        ValueTask ChangeAsync<T>(T item);
        ValueTask ChangeRangeAsync<T>(IEnumerable<T> items);

        ValueTask DeleteAsync<T>(string id);
        ValueTask DeleteRangeAsync<T>(IEnumerable<string> ids);

        ValueTask<FrpResponse?> QueryAsync(FrpQueryRequest qr);
        ValueTask<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predExpr);
        ValueTask<T?> FirstOrDefaultAsync<T>(IFrpQueryable<T> q);
        IFrpQueryable<T> Where<T>(Expression<Func<T, bool>> predExpr);
    }
}
