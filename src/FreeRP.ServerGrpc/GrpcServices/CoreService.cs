using FreeRP.FrpServices;
using FreeRP.ServerCore;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerGrpc
{
    public class CoreService : FreeRP.CoreService.CoreServiceBase
    {
        private readonly FrpSettings _frpSettings;
        private readonly IFrpDataService _frpData;
        private readonly IFrpAuthService _authService;
        private readonly ConnectResponse _res;

        public CoreService(FrpSettings conf, IFrpDataService frpDataService, IFrpAuthService authService)
        {
            _frpSettings = conf;
            _frpData = frpDataService;
            _authService = authService;

            _res = new ConnectResponse()
            {
                WithPassword = _frpSettings.WithPassword,
                PasswordLength = _frpSettings.PasswordLength,
                GrpcMessageSize = _frpSettings.GrpcMessageSize
            };
        }

        public override Task<ConnectResponse> Connect(Empty request, ServerCallContext context)
        {
            return Task.FromResult(_res);
        }

        public override Task<LoginResponse> Login(User request, ServerCallContext context)
        {
            if (_frpSettings.Admin == request.Email && _frpSettings.AdminPassword == request.Password)
            {
                return Task.FromResult(new LoginResponse()
                {
                    ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.JwtExpireHours)),
                    Token = _authService.GenerateJwtToken(request, true),
                    User = new()
                    {
                        FirstName = "FreeRP",
                        LastName = "Admin",
                        Theme = new Theme()
                        {
                            BaseLayerLuminance = 0,
                            AccentBaseColor = "#1E90FF"
                        }
                    }
                });
            }
            else if (_frpData.FrpUserService.GetUserByEmail(request.Email) is User u)
            {
                request.UserId = u.UserId;
                if (_frpData.FrpUserService.CheckPassword(request))
                {
                    return Task.FromResult(new LoginResponse()
                    {
                        ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.JwtExpireHours)),
                        Token = _authService.GenerateJwtToken(request, false),
                        User = u
                    });
                }
            }

            return Task.FromResult(new LoginResponse());
        }

        public override Task<LoginResponse> LoginWithToken(TokenRequest request, ServerCallContext context)
        {
            if (_authService.GetUserFromToken(request.Token) is User user)
            {
                return Task.FromResult(new LoginResponse()
                {
                    ExpirationDate = FrpUtcDateTime.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.JwtExpireHours)),
                    Token = request.Token,
                    User = user
                });
            }

            return Task.FromResult(new LoginResponse());
        }

        public override Task<PingData> CheckConnection(PingData request, ServerCallContext context)
        {
            request.Tick = DateTime.UtcNow.Ticks;
            return Task.FromResult(request);
        }

        [Authorize]
        public override Task<PingData> CheckAuthorize(PingData request, ServerCallContext context)
        {
            request.Tick = DateTime.UtcNow.Ticks;
            return Task.FromResult(request);
        }
    }
}
