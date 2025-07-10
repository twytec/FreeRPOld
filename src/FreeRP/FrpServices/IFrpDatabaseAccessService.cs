using FreeRP.Database;
using FreeRP.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpDatabaseAccessService
    {
        public const string RecordTypeDatabaseAccess = "FrpDatabaseAccess";

        /// <summary>
        /// Returns the database permissions for the user
        /// </summary>
        /// <param name="db"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpDatabasePermissions> GetDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService);

        /// <summary>
        /// Opens the database unit if it exists and the user has access to it
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> OpenDatabaseAsync(string frpDatabaseId, IFrpAuthService authService);

        /// <summary>
        /// Saves all changes in the database unit if the database is open
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> SaveChangesAsync(string frpDatabaseId, IFrpAuthService authService);

        /// <summary>
        /// Closes the database unit if it is open and releases all resources
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> CloseDatabaseAsync(string frpDatabaseId, IFrpAuthService authService);

        /// <summary>
        /// Adds the item to the database unit
        /// </summary>
        /// <param name="dataRequest"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService);

        /// <summary>
        /// Updates the item in the database unit
        /// </summary>
        /// <param name="dataRequest"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService);

        /// <summary>
        /// Delets the item in the database unit
        /// </summary>
        /// <param name="dataRequest"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService);

        /// <summary>
        /// Returns the first element of a sequence that satisfies a specified condition or a default value if no such element is found.
        /// </summary>
        /// <param name="queryRequest"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> FirstOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService);

        /// <summary>
        /// Searching for items via a query
        /// </summary>
        /// <param name="queryRequest"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ListOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetDatabaseAccessAsync(FrpLog log, IFrpAuthService authService);
    }
}
