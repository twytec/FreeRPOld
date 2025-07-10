using FreeRP.FrpServices;
using FreeRP.ServerCore;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerGrpc
{
    public class UserService(IFrpDataService appData, IFrpAuthService authService) : FreeRP.UserService.UserServiceBase
    {
        private readonly IFrpDataService _appData = appData;
        private readonly IFrpAuthService _authService = authService;

        [Authorize]
        public override Task<UserData> GetData(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new UserData() { User = _authService.User });
        }

        [Authorize]
        public override async Task<Response> UserChange(User request, ServerCallContext context)
        {
            return await _appData.FrpUserService.ChangeUserAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> UserChangePassword(User request, ServerCallContext context)
        {
            return await _appData.FrpUserService.ChangePasswordAsync(request, _authService);
        }
    }
}
