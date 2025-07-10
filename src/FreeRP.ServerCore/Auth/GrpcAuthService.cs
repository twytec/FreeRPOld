using FreeRP.Auth;
using FreeRP.FrpServices;
using FreeRP.User;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Auth
{
    public class GrpcAuthService(IFrpAuthService auth) : FreeRP.Auth.GrpcAuthService.GrpcAuthServiceBase
    {
        private readonly IFrpAuthService _auth = auth;

        public override async Task<FrpConnectResponse> Connect(FrpStringValueRequest request, ServerCallContext context)
        {
            return await _auth.ConnectAsync(request.Val);
        }

        public override async Task<FrpLoginResponse> Login(FrpUser request, ServerCallContext context)
        {
            return await _auth.LoginAsync(request);
        }

        public override async Task<FrpLoginResponse> LoginWithToken(FrpStringValueRequest request, ServerCallContext context)
        {
            return await _auth.LoginWithTokenAsync(request.Val);
        }

        [Authorize]
        public override async Task<FrpToken> GetToken(Empty request, ServerCallContext context)
        {
            return await _auth.GetTokenAsync();
        }

        [Authorize]
        public override async Task<FrpPingData> PingServer(FrpPingData request, ServerCallContext context)
        {
            return await _auth.PingServerAsync(request);
        }
    }
}
