using FreeRP.Log;
using FreeRP.User;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public partial interface IFrpUserService
    {
        public const string RecordTypeUser = "FrpUser";
        public const string ClaimName = "name";
        public const string ClaimAdmin = "admin";

        /// <summary>
        /// Get user by e-mail
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ValueTask<FrpUser?> GetUserByEmailAsync(string name);

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ValueTask<FrpUser?> GetUserByIdAsync(string id);

        /// <summary>
        /// Is user by ID exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<bool> IsUserByIdExistsAsync(string id);

        /// <summary>
        /// Get all users and API-User
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpUser>> GetAllUsersAsync();

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpUser>> GetUsersAsync();

        /// <summary>
        /// Get all API-users
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpUser>> GetApiUsersAsync();

        #region User

        /// <summary>
        /// Adds the user if it does exists
        /// </summary>
        /// <param name="u"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddUserAsync(FrpUser u, IFrpAuthService authService);

        /// <summary>
        /// Changes the user, if it exists
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeUserAsync(FrpUser user, IFrpAuthService authService);

        /// <summary>
        /// Delets the user, if it exists
        /// </summary>
        /// <param name="u"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteUserAsync(FrpUser u, IFrpAuthService authService);

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeUserPasswordAsync(FrpUser user, IFrpAuthService authService);

        #endregion

        #region ApiUser

        /// <summary>
        /// Adds the API-users if it does exists
        /// </summary>
        /// <param name="u"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddApiUserAsync(FrpUser u, IFrpAuthService authService);

        /// <summary>
        /// Changes the API-users, if it exists
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeApiUserAsync(FrpUser user, IFrpAuthService authService);

        /// <summary>
        /// Delets the API-users, if it exists
        /// </summary>
        /// <param name="u"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteApiUserAsync(FrpUser u, IFrpAuthService authService);

        /// <summary>
        /// Changes the API-users token, if it exists
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangeApiUserTokenAsync(FrpUser user, IFrpAuthService authService);

        /// <summary>
        /// Get API-user token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> GetApiUserTokenAsync(FrpUser user, IFrpAuthService authService);

        #endregion

        #region Password

        public static FrpErrorType ValidatePassword(string password, int passwortLength)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < passwortLength)
            {
                return FrpErrorType.ErrorPasswordToShort;
            }

            if (RegexNumber().IsMatch(password) == false)
            {
                return FrpErrorType.ErrorPasswordNumber;
            }

            if (RegexUpperChar().IsMatch(password) == false)
            {
                return FrpErrorType.ErrorPasswordUpperChar;
            }

            if (RegexLowerChar().IsMatch(password) == false)
            {
                return FrpErrorType.ErrorPasswordLowerChar;
            }

            if (RegexSymbols().IsMatch(password) == false)
            {
                return FrpErrorType.ErrorPasswordSymbols;
            }

            return FrpErrorType.ErrorNone;
        }

        [GeneratedRegex("[0-9]{1,}")]
        private static partial Regex RegexNumber();

        [GeneratedRegex("[A-Z]{1,}")]
        private static partial Regex RegexUpperChar();

        [GeneratedRegex("[a-z]{1,}")]
        private static partial Regex RegexLowerChar();
        [GeneratedRegex("[!@#$%^&*()_+=\\[{\\]};:<>|./?,-]{1,}")]
        private static partial Regex RegexSymbols();

        #endregion

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name="log"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetUserAsync(FrpLog log, IFrpAuthService authService);
    }
}
