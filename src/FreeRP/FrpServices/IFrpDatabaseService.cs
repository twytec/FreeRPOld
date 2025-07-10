using FreeRP.Database;
using FreeRP.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpDatabaseService
    {
        public const string RecordTypeDatabase = "FrpDatabases";
        public const string UriSchemeDatabase = "db";
        public const string DatabasePrimaryKeyName = "id";
        public const string DatabasesFolderName = "db";

        /// <summary>
        /// Returns all databases
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpDatabase>> GetAllDatabasesAsync();

        /// <summary>
        /// Returns the database by Id if exist
        /// </summary>
        /// <param name="databaseId"></param>
        /// <returns></returns>
        ValueTask<FrpDatabase?> GetDatabaseByIdAsync(string databaseId);

        /// <summary>
        /// Adds the database if it does not exist
        /// </summary>
        /// <param name="database"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddDatabaseAsync(FrpDatabase database, IFrpAuthService authService);

        /// <summary>
        /// Changes the database if exists
        /// </summary>
        /// <param name="ndb"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService);

        /// <summary>
        /// Deletes the database if exists
        /// </summary>
        /// <param name="ndb"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetDatabaseAsync(FrpLog record, IFrpAuthService authService);

    }
}
