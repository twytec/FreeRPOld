using FreeRP.Auth;
using FreeRP.FrpServices;
using FreeRP.Localization;
using FreeRP.Plugins;
using FreeRP.Role;
using FreeRP.ServerCore.User;
using FreeRP.Settings;
using FreeRP.User;
using Grpc.Net.Client;
using System.Security.Claims;

namespace FreeRP.ServerCore.Auth
{
    public class FrpAuthService(IFrpDataService ds, FrpSettings frpSettings, FrpLocalizationService i18n) : IFrpAuthService
    {
        public FrpUser User { get; set; } = new();
        public List<FrpRole> Roles { get; } = [];
        public List<FrpPlugin> Plugins { get; } = [];
        public FrpLocalizationService I18n { get; set; } = i18n;
        public bool IsAdmin { get; set; }
        public bool IsLogin { get; set; }
        public FrpToken? Token { get; set; }
        public FrpConnectResponse FrpConnectResponse { get; set; } = new() {
            GrpcMessageSizeInByte = frpSettings.GrpcSettings.GrpcMessageSizeInByte,
            MinPasswordLength = frpSettings.LoginSettings.MinPasswordLength,
            Passwordless = frpSettings.LoginSettings.Passwordless,
            UseRefreshToken = frpSettings.LoginSettings.UseRefreshToken
        };

        public GrpcChannel? Channel => throw new NotImplementedException();

        private readonly IFrpDataService _ds = ds;
        private readonly FrpSettings _frpSettings = frpSettings;
        private readonly FrpUser _admin = frpSettings.Admin;

        public string? CurrentServer { get; }
        public event EventHandler<IFrpAuthService>? Connected;
        public event EventHandler<IFrpAuthService>? Disconnected;

        public async Task SetUserAsync(ClaimsPrincipal claims)
        {
            Roles.Clear();
            if (claims.FindFirst(x => x.Type == IFrpUserService.ClaimName) is Claim c)
            {
                if (await _ds.FrpUserService.GetUserByEmailAsync(c.Value) is FrpUser u)
                {
                    await SetUserAsync(u);
                }
                else if (claims.FindFirst(x => x.Type == IFrpUserService.ClaimAdmin) is Claim ac)
                {
                    IsAdmin = true;
                    await SetUserAsync(_admin);
                }
            }
        }

        public async Task SetUserAsync(FrpUser u)
        {
            User = u;
            IsLogin = true;
            if (IsAdmin == false)
            {
                Roles.AddRange(await _ds.FrpRoleService.GetUserRolesAsync(User.UserId));
            }

            I18n.SetText(User.Language);
        }

        public ValueTask<FrpConnectResponse> ConnectAsync(string host)
        {
            return ValueTask.FromResult(FrpConnectResponse);
        }

        public async ValueTask<FrpLoginResponse> LoginAsync(FrpUser user)
        {
            if (_ds.FrpUserService is FrpUserService f)
            {
                if (user.Email == _admin.Email && user.Password == _admin.Password)
                {
                    var ac = _admin.Clone();
                    ac.Password = "";
                    return new FrpLoginResponse()
                    {
                        IsAdmin = true,
                        User = ac,
                        Token = new()
                        {
                            Token = f.GenerateUserToken(user, true),
                            ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.LoginSettings.TokenValidityInHours))
                        }
                    };
                }
                else if (await f.GetUserByEmailAndPasswortAsync(user) is FrpUser u)
                {
                    var res = new FrpLoginResponse()
                    {
                        User = u,
                        Token = new()
                        {
                            Token = f.GenerateUserToken(u, false),
                            ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.LoginSettings.TokenValidityInHours))
                        }
                    };
                    res.Roles.AddRange(await _ds.FrpRoleService.GetUserRolesAsync(u.UserId));
                    return res;
                }
            }

            return new FrpLoginResponse()
            {
                ErrorType = FrpErrorType.ErrorUserNotExist,
                ErrorMessage = I18n.Text.ErrorUserNotExist
            };
        }

        public async ValueTask<FrpLoginResponse> LoginWithTokenAsync(string token)
        {
            if (_ds.FrpUserService is FrpUserService u && await u.GetLoginFromTokenAsync(token) is FrpLoginResponse res)
            {
                if (res.User.Email == _admin.Email)
                {
                    res.IsAdmin = true;
                }
                else
                {
                    res.IsAdmin = false;
                    res.Roles.AddRange(await _ds.FrpRoleService.GetUserRolesAsync(res.User.UserId));
                }

                return res;
            }

            return new FrpLoginResponse()
            {
                ErrorType = FrpErrorType.ErrorNotFound,
                ErrorMessage = I18n.Text.ErrorNotFound
            };
        }

        public ValueTask<FrpToken> GetTokenAsync()
        {
            if (_ds.FrpUserService is FrpUserService f)
            {
                if (_frpSettings.LoginSettings.UseRefreshToken)
                {
                    FrpToken token = new()
                    {
                        Token = f.GenerateUserToken(User, false, true),
                        ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddMinutes(_frpSettings.LoginSettings.ShortTimeTokenValidityInMinutes))
                    };
                    return ValueTask.FromResult(token);
                }
                else if (Token is not null) 
                { 
                    return ValueTask.FromResult(Token);
                }
            }

            return ValueTask.FromResult(new FrpToken());
        }

        public ValueTask<FrpPingData> PingServerAsync(FrpPingData pingData)
        {
            return ValueTask.FromResult(pingData);
        }

        
    }
}
