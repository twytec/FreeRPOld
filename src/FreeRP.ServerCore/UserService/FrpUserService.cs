using FreeRP.Auth;
using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.Log;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Database;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using FreeRP.User;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeRP.ServerCore.User
{
    public class FrpUserService : IFrpUserService
    {
        private readonly ConcurrentDictionary<string, FrpUser> _allUsers = [];
        private readonly ConcurrentDictionary<string, FrpUser> _users = [];
        private readonly ConcurrentDictionary<string, FrpUser> _apiUsers = [];
        private readonly ConcurrentDictionary<string, string> _userPasswords = new();

        private readonly IFrpDataService _ds;
        private readonly IFrpLogService _logService;
        private readonly FrpDatabase _db;
        private readonly ILogger _log;
        private readonly FrpSettings _frpSettings;
        private readonly IFrpAuthService _systemUser;

        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidations;
        private readonly SymmetricSecurityKey _tokenKey;

        public FrpUserService(IFrpDataService ds, FrpSettings frpSettings, IFrpLogService logService, FrpDatabase db, ILogger logger)
        {
            _ds = ds;
            _logService = logService;
            _db = db;
            _log = logger;
            _frpSettings = frpSettings;
            _systemUser = new FrpAuthService(ds, frpSettings, new()) { IsAdmin = true, User = _frpSettings.System };

            _tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_frpSettings.LoginSettings.TokenSigningKey));
            _tokenHandler = new JwtSecurityTokenHandler();
            _tokenValidations = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _tokenKey,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }

        internal async Task InitAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            try
            {
                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var users = await db.ListOrDefaultAsync(x => x.RecordType == IFrpUserService.RecordTypeUser);
                if (users.Any())
                {
                    foreach (var item in users)
                    {
                        if (GrpcJson.GetModel<FrpUser>(item.DataAsJson) is FrpUser u)
                        {
                            _userPasswords[u.UserId] = u.Password;
                            _allUsers[u.UserId] = u;

                            if (u.IsApi)
                            {
                                _apiUsers[u.UserId] = u;
                            }
                            else
                            {
                                _users[u.UserId] = u;
                            }

                            u.Password = "";
                        }
                    }
                }
                await db.CloseDatabaseAsync();
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(LoadUsers), ex, _systemUser);
                _log.LogError(ex, "Load users from Database");
            }
        }

        #region Get

        public ValueTask<IEnumerable<FrpUser>> GetAllUsersAsync() => ValueTask.FromResult(_allUsers.Values.AsEnumerable());
        public ValueTask<IEnumerable<FrpUser>> GetUsersAsync() => ValueTask.FromResult(_users.Values.AsEnumerable());
        public ValueTask<IEnumerable<FrpUser>> GetApiUsersAsync() => ValueTask.FromResult(_apiUsers.Values.AsEnumerable());

        public ValueTask<FrpUser?> GetUserByEmailAsync(string name) => ValueTask.FromResult(_allUsers.Values.FirstOrDefault(x => x.Email == name));
        public ValueTask<FrpUser?> GetUserByIdAsync(string id) => ValueTask.FromResult(_allUsers.TryGetValue(id, out FrpUser? value) ? value : null);
        public ValueTask<bool> IsUserByIdExistsAsync(string id) => ValueTask.FromResult(_allUsers.ContainsKey(id));

        #endregion

        #region User

        public async ValueTask<FrpResponse> AddUserAsync(FrpUser u, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(u) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                u.IsApi = false;
                u.Email = u.Email.ToLower();
                if (IsEmailValid(u.Email) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorEmailInvalid, authService.I18n);
                }

                if (await GetUserByEmailAsync(u.Email) is not null)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorUserExist, authService.I18n);
                }

                var passVal = IFrpUserService.ValidatePassword(u.Password, _frpSettings.LoginSettings.MinPasswordLength);
                if (passVal != FrpErrorType.ErrorNone)
                {
                    return FrpResponse.Create(passVal, authService.I18n);
                }
                u.Password = GetPasswordHash(u.Password);

                if (string.IsNullOrWhiteSpace(u.Language))
                    u.Language = "en";

                u.UserId = FrpId.NewId();

                _allUsers[u.UserId] = u;
                _users[u.UserId] = u;
                _userPasswords[u.UserId] = u.Password;

                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var r = IFrpRecordContext.GetRecord(authService.User.UserId, u.UserId, IFrpUserService.RecordTypeUser, u.UserId, GrpcJson.GetJson(u));
                await db.AddAsync(r);
                await db.SaveChangesAndCloseAsync();

                await AddLogAsync(IFrpLogService.ActionAdd, r, authService);
                u.Password = "";

                return FrpResponse.Create(u);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddUserAsync), ex, authService);
                _log.LogError(ex, nameof(AddUserAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeUserAsync(FrpUser user, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(user) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin || user.UserId == authService.User.UserId)
                {
                    if (_allUsers.TryGetValue(user.UserId, out var u) && u.IsApi == false)
                    {
                        user.IsApi = false;
                        user.Email = user.Email.ToLower();
                        user.Password = _userPasswords[user.UserId];

                        if (IsEmailValid(user.Email) == false)
                        {
                            return FrpResponse.Create(FrpErrorType.ErrorEmailInvalid, authService.I18n);
                        }

                        var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                        var r = await db.FirstOrDefaultAsync(x => x.RecordId == user.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                        if (r is not null)
                        {
                            await db.DeleteAsync(r);
                            await db.AddAsync(
                            IFrpRecordContext.GetRecord(authService.User.UserId, user.UserId, IFrpUserService.RecordTypeUser, r.Owner, GrpcJson.GetJson(user)));

                            await db.SaveChangesAndCloseAsync();

                            _allUsers[user.UserId] = user;
                            _users[user.UserId] = user;

                            await AddLogAsync(IFrpLogService.ActionChange, r, authService);
                            user.Password = "";

                            return FrpResponse.ErrorNone();
                        }
                    }

                    return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
                }
                else
                {
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                }
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeUserAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeUserAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeUserPasswordAsync(FrpUser user, IFrpAuthService authService)
        {
            try
            {
                if (authService.IsAdmin || user.UserId == authService.User.UserId)
                {
                    var passVal = IFrpUserService.ValidatePassword(user.Password, _frpSettings.LoginSettings.MinPasswordLength);
                    if (passVal is not FrpErrorType.ErrorNone)
                    {
                        return FrpResponse.Create(passVal, authService.I18n);
                    }

                    if (_allUsers.TryGetValue(user.UserId, out var u) && u.IsApi == false)
                    {
                        var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                        var r = await db.FirstOrDefaultAsync(x => x.RecordId == user.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                        if (r is not null)
                        {
                            await db.DeleteAsync(r);

                            var clone = u.Clone();
                            clone.Password = GetPasswordHash(user.Password);
                            await db.AddAsync(
                                IFrpRecordContext.GetRecord(authService.User.UserId, user.UserId, IFrpUserService.RecordTypeUser, r.Owner, GrpcJson.GetJson(clone)));
                            await db.SaveChangesAndCloseAsync();

                            await AddLogAsync(IFrpLogService.ActionChangePasswort, r, authService);

                            _userPasswords[clone.UserId] = clone.Password;
                            clone.Password = "";

                            return FrpResponse.ErrorNone();
                        }
                    }

                    return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
                }
                else
                {
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);
                }
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeUserPasswordAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeUserPasswordAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteUserAsync(FrpUser u, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(u) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_allUsers.TryRemove(u.UserId, out _) && _userPasswords.TryRemove(u.UserId, out _))
                {
                    _users.TryRemove(u.UserId, out _);

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == u.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);
                        await db.SaveChangesAndCloseAsync();

                        await AddLogAsync(IFrpLogService.ActionDelete, r, authService);
                    }

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteUserAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteUserAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        #endregion

        #region ApiUser

        public async ValueTask<FrpResponse> AddApiUserAsync(FrpUser u, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(u) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                u.IsApi = true;
                u.Email = u.Email.ToLower();
                if (IsEmailValid(u.Email) is false)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorEmailInvalid, authService.I18n);
                }

                if (await GetUserByEmailAsync(u.Email) is not null)
                {
                    return FrpResponse.Create(FrpErrorType.ErrorUserExist, authService.I18n);
                }

                if (string.IsNullOrWhiteSpace(u.Language))
                    u.Language = "en";

                if (u.UtcDateTime.UnixTimeSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    u.UtcDateTime = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddYears(1));

                u.Password = GenerateApiUserToken(u, u.UtcDateTime.ToDateTime());
                u.UserId = FrpId.NewId();

                var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                var r = IFrpRecordContext.GetRecord(authService.User.UserId, u.UserId, IFrpUserService.RecordTypeUser, u.UserId, GrpcJson.GetJson(u));
                await db.AddAsync(r);
                await db.SaveChangesAndCloseAsync();

                _allUsers[u.UserId] = u;
                _apiUsers[u.UserId] = u;
                _userPasswords[u.UserId] = u.Password;
                u.Password = "";

                await AddLogAsync(IFrpLogService.ActionAdd, r, authService);

                return FrpResponse.Create(u);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(AddApiUserAsync), ex, authService);
                _log.LogError(ex, nameof(AddApiUserAsync));

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeApiUserAsync(FrpUser user, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(user) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_allUsers.TryGetValue(user.UserId, out var u) && u.IsApi == true)
                {
                    user.IsApi = true;
                    user.Email = user.Email.ToLower();
                    user.Password = _userPasswords[user.UserId];

                    if (IsEmailValid(user.Email) == false)
                    {
                        return FrpResponse.Create(FrpErrorType.ErrorEmailInvalid, authService.I18n);
                    }

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == user.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);

                        await db.AddAsync(
                            IFrpRecordContext.GetRecord(authService.User.UserId, user.UserId, IFrpUserService.RecordTypeUser, r.Owner, GrpcJson.GetJson(user)));

                        await db.SaveChangesAndCloseAsync();

                        _allUsers[user.UserId] = user;
                        _apiUsers[user.UserId] = user;
                        user.Password = "";

                        await AddLogAsync(IFrpLogService.ActionChange, r, authService);

                        return FrpResponse.ErrorNone();
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeApiUserAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeApiUserAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> DeleteApiUserAsync(FrpUser u, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(u) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_allUsers.TryRemove(u.UserId, out _))
                {
                    _apiUsers.TryRemove(u.UserId, out _);

                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == u.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);
                        await db.SaveChangesAndCloseAsync();
                        await AddLogAsync(IFrpLogService.ActionDelete, r, authService);
                    }

                    return FrpResponse.ErrorNone();
                }

                return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(DeleteApiUserAsync), ex, authService);
                _log.LogError(ex, nameof(DeleteApiUserAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> GetApiUserTokenAsync(FrpUser user, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(user) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_allUsers.TryGetValue(user.UserId, out var u) && u.IsApi == true)
                {
                    if (_userPasswords.TryGetValue(user.UserId, out var pass))
                    {
                        return FrpResponse.Create(pass);
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(GetApiUserTokenAsync), ex, authService);
                _log.LogError(ex, nameof(GetApiUserTokenAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        public async ValueTask<FrpResponse> ChangeApiUserTokenAsync(FrpUser user, IFrpAuthService authService)
        {
            try
            {
                if (IsAdminOrSystemUser(user) is FrpResponse eres)
                    return eres;

                if (authService.IsAdmin == false)
                    return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

                if (_allUsers.TryGetValue(user.UserId, out var u) && u.IsApi == true)
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    var r = await db.FirstOrDefaultAsync(x => x.RecordId == user.UserId && x.RecordType == IFrpUserService.RecordTypeUser);
                    if (r is not null)
                    {
                        await db.DeleteAsync(r);

                        var clone = u.Clone();
                        clone.Password = GenerateApiUserToken(u, user.UtcDateTime.ToDateTime());

                        await db.AddAsync(
                                IFrpRecordContext.GetRecord(authService.User.UserId, user.UserId, IFrpUserService.RecordTypeUser, r.Owner, GrpcJson.GetJson(clone)));

                        await db.SaveChangesAndCloseAsync();

                        _userPasswords[clone.UserId] = clone.Password;
                        user.Password = "";

                        await AddLogAsync(IFrpLogService.ActionChangePasswort, r, authService);
                        return FrpResponse.ErrorNone();
                    }
                }

                return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, authService.I18n);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ChangeApiUserTokenAsync), ex, authService);
                _log.LogError(ex, nameof(ChangeApiUserTokenAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        #endregion

        public async ValueTask<FrpResponse> ResetUserAsync(FrpLog log, IFrpAuthService authService)
        {
            if (authService.IsAdmin == false)
                return FrpResponse.Create(FrpErrorType.ErrorAccessDenied, authService.I18n);

            try
            {
                if (GrpcJson.TryGetModel<FrpRecord>(log.Record, out var r))
                {
                    var db = FrpRecordContextFactory.Create(_frpSettings, _db);
                    if (
                        log.Action == IFrpLogService.ActionAdd || 
                        log.Action == IFrpLogService.ActionChange || 
                        log.Action == IFrpLogService.ActionChangePasswort)
                    {
                        var old = await db.FirstOrDefaultAsync(x => x.RecordId == r.RecordId);
                        if (old is not null)
                            await db.DeleteAsync(old);
                    }

                    if (log.Action == IFrpLogService.ActionAdd)
                    {
                        _allUsers.TryRemove(r.RecordId, out _);
                        _apiUsers.TryRemove(r.RecordId, out _);
                        _users.TryRemove(r.RecordId, out _);

                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    else if (GrpcJson.TryGetModel<FrpUser>(r.DataAsJson, out var a))
                    {
                        _allUsers[r.RecordId] = a;
                        _userPasswords[r.RecordId] = a.Password;

                        if (a.IsApi) _apiUsers[r.RecordId] = a;
                        else _users[r.RecordId] = a;

                        await db.AddAsync(r);
                        await db.SaveChangesAndCloseAsync();
                        return FrpResponse.Create(FrpErrorType.ErrorNone, authService.I18n);
                    }
                    await db.CloseDatabaseAsync();
                }

                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
            catch (Exception ex)
            {
                await AddExceptionAsync(nameof(ResetUserAsync), ex, authService);
                _log.LogError(ex, nameof(ResetUserAsync));
                return FrpResponse.ErrorUnknown(authService.I18n.Text.ErrorUnknown);
            }
        }

        #region Helpers

        internal async ValueTask<FrpUser?> GetUserByEmailAndPasswortAsync(FrpUser user)
        {
            if (
                await GetUserByEmailAsync(user.Email) is FrpUser u &&
                _userPasswords.TryGetValue(u.UserId, out string? p) &&
                GetPasswordHash(user.Password) == p)
            {
                return u;
            }

            return null;
        }

        internal async ValueTask<FrpLoginResponse?> GetLoginFromTokenAsync(string token)
        {
            try
            {
                FrpLoginResponse res = new();
                var claims = _tokenHandler.ValidateToken(token, _tokenValidations, out var st);
                
                var name = claims.FindFirst(x => x.Type == IFrpUserService.ClaimName);
                if (name != null)
                {
                    res.User = await GetUserByEmailAsync(name.Value);
                    res.Token = new FrpToken()
                    {
                        Token = token,
                        ExpirationDate = FrpUtcDateTime.FromDateTime(st.ValidTo)
                    };

                    return res;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        internal string GenerateUserToken(FrpUser user, bool isAdmin, bool shortTime = false)
        {
            List<Claim> c =
            [
                new Claim(IFrpUserService.ClaimName, user.Email)
            ];

            if (isAdmin)
                c.Add(new Claim(IFrpUserService.ClaimAdmin, ""));

            DateTime dt;
            if (shortTime)
                dt = DateTime.UtcNow.AddMinutes(_frpSettings.LoginSettings.ShortTimeTokenValidityInMinutes);
            else
                dt = DateTime.UtcNow.AddHours(_frpSettings.LoginSettings.TokenValidityInHours);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(c),
                Expires = dt,
                SigningCredentials = new SigningCredentials(_tokenKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        internal string GenerateAdminToken(FrpUser user)
        {
            List<Claim> c =
            [
                new Claim(IFrpUserService.ClaimAdmin, user.Email)
            ];

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(c),
                Expires = DateTime.UtcNow.AddHours(_frpSettings.LoginSettings.TokenValidityInHours),
                SigningCredentials = new SigningCredentials(_tokenKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        internal string GenerateApiUserToken(FrpUser user, DateTime dt)
        {
            List<Claim> c =
            [
                new Claim(IFrpUserService.ClaimName, user.Email)
            ];

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(c),
                Expires = dt,
                SigningCredentials = new SigningCredentials(_tokenKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        internal string GetPasswordHash(string password)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.UTF8.GetBytes(_frpSettings.LoginSettings.PasswordSigningKey),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
        }

        public static bool IsEmailValid(string email)
        {
            return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }

        private FrpResponse? IsAdminOrSystemUser(FrpUser u)
        {
            if (u.Email == _frpSettings.Admin.Email || u.Email == _frpSettings.System.Email)
                return new FrpResponse() { ErrorType = FrpErrorType.ErrorAccessDenied };

            return null;
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
                Location = $"{nameof(FrpUserService)}/{location}",
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
                Location = $"{nameof(FrpUserService)}/{location}",
                Message = ex.Message,
                UserId = authService.User.UserId,
                Val1 = ex.StackTrace
            }, authService);
        }
    }
}
