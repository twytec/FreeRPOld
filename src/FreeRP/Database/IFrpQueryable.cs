using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Database
{
    /// <summary>
    /// An IQueryable-like class to write fluent query in objects in database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFrpQueryable<T>
    {
        IEnumerable<FrpQuery> GetQueries { get; }

        /// <summary>
        /// Execute query and return results as a List
        /// </summary>
        Task<IEnumerable<T>> ToListAsync();

        /// <summary>
        /// Execute query and return results as an Array
        /// </summary>
        Task<IEnumerable<T>> ToArrayAsync();

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        /// Default value if source is empty or if no element passes the test specified by predicate; 
        /// otherwise, the first element in source that passes the test specified by predicate
        /// </returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predExpr);

        /// <summary>
        /// Filters a sequence of objects based on a predicate expression
        /// </summary>
        /// <returns>
        /// Return a IFrpQueryable to build more complex queries
        /// </returns>
        IFrpQueryable<T> Where(Expression<Func<T, bool>> predExpr);

        /// <summary>
        /// Bypasses a specified number of objects in resultset and retun the remaining objects
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        IFrpQueryable<T> Skip(int offset);

        /// <summary>
        /// Return a specified number of contiguous objects from start of resultset
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IFrpQueryable<T> Take(int count);
    }
}
