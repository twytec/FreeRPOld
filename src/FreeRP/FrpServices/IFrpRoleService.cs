using FreeRP.Log;
using FreeRP.Role;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpRoleService
    {
        public const string RecordTypeRole = "FrpRole";
        public const string RecordTypeUserInRole = "FrpUserInRole";

        /// <summary>
        /// Is role by Id exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<bool> IsRoleByIdExistsAsync(string id);

        /// <summary>
        /// Is role by name exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<bool> IsRoleByNameExistsAsync(string name);

        /// <summary>
        /// Is user in role
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<bool> IsUserInRoleAsync(string userId, string roleId);

        /// <summary>
        /// Get role by id
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpRole?> GetRoleByIdAsync(string id);

        /// <summary>
        /// Get all roles
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpRole>> GetAllRolesAsync();

        /// <summary>
        /// Get all user in roles
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpUserInRole>> GetAllUserInRolesAsync();

        /// <summary>
        /// Get user roles
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpRole>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Adds the role if it does exists
        /// </summary>
        /// <param name="role"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddRoleAsync(FrpRole role, IFrpAuthService authService);

        /// <summary>
        /// Changes the role, if it exists
        /// </summary>
        /// <param name="role"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeRoleAsync(FrpRole role, IFrpAuthService authService);

        /// <summary>
        /// Delets the role, if it exists
        /// </summary>
        /// <param name="role"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteRoleAsync(FrpRole role, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="record"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetRoleAsync(FrpLog record, IFrpAuthService authService);

        /// <summary>
        /// Adds the user to the role if the role and the user exist
        /// </summary>
        /// <param name="userInRole"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddUserToRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService);

        /// <summary>
        /// Delets the user from the role if the role and the user exist
        /// </summary>
        /// <param name="userInRole"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteUserFromRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="log"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetUserInRoleAsync(FrpLog log, IFrpAuthService authService);
    }
}
