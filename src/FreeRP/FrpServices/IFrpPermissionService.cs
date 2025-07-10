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
    public interface IFrpPermissionService
    {
        public const string RecordTypePermission = "FrpPermissions";

        /// <summary>
        /// Returns the permission by Id if exist
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<FrpPermission?> GetPermissionByIdAsync(string id);

        /// <summary>
        /// returns the user's permission for the URI
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpPermission> GetUserContentPermissionAsync(FrpUri uri, IFrpAuthService authService);

        /// <summary>
        /// returns the user's permissions for the database
        /// </summary>
        /// <param name="db"></param>
        /// <param name="authService"></param>
        ValueTask<IEnumerable<FrpPermission>> GetUserDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService);

        /// <summary>
        /// Returns the database permissions by database Id if exist
        /// </summary>
        /// <param name="databaseId"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpPermission>> GetDatabasePermissionsAsync(string databaseId);

        /// <summary>
        /// Returns the content permissions by URI if exist
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpPermission>> GetContentPermissionsAsync(string uri);

        /// <summary>
        /// Adds the permission if it does not exist
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddPermissionAsync(FrpPermission ac, IFrpAuthService authService);

        /// <summary>
        /// Change the permission if exist
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangePermissionAsync(FrpPermission ac, IFrpAuthService authService);

        /// <summary>
        /// Delete the permission if exist
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeletePermissionAsync(FrpPermission ac, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="log"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetPermissionAysnc(FrpLog log, IFrpAuthService authService);
    }
}
