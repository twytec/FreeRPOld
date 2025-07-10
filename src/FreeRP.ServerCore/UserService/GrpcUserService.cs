using FreeRP.FrpServices;
using FreeRP.Log;
using FreeRP.User;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.User
{
    public class GrpcUserService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.User.GrpcUserService.GrpcUserServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> GetUserByEmail(FrpStringValueRequest request, ServerCallContext context)
        {
            if (await _ds.FrpUserService.GetUserByEmailAsync(request.Val) is FrpUser r)
                return FrpResponse.Create(r);

            return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, _auth.I18n);
        }

        [Authorize]
        public override async Task<FrpResponse> GetUserById(FrpStringValueRequest request, ServerCallContext context)
        {
            if (await _ds.FrpUserService.GetUserByIdAsync(request.Val) is FrpUser r)
                return FrpResponse.Create(r);

            return FrpResponse.Create(FrpErrorType.ErrorUserNotExist, _auth.I18n);
        }

        [Authorize]
        public override async Task<FrpResponse> IsUserByIdExists(FrpStringValueRequest request, ServerCallContext context)
        {
            return FrpResponse.CreateBool(await _ds.FrpUserService.IsUserByIdExistsAsync(request.Val));
        }

        [Authorize]
        public override async Task<FrpResponse> GetAllUsers(Empty request, ServerCallContext context)
        {
            FrpUsers users = new();
            users.Users.AddRange(await _ds.FrpUserService.GetAllUsersAsync());
            return FrpResponse.Create(users);
        }

        [Authorize]
        public override async Task<FrpResponse> GetUsers(Empty request, ServerCallContext context)
        {
            FrpUsers users = new();
            users.Users.AddRange(await _ds.FrpUserService.GetUsersAsync());
            return FrpResponse.Create(users);
        }

        [Authorize]
        public override async Task<FrpResponse> GetApiUsers(Empty request, ServerCallContext context)
        {
            FrpUsers users = new();
            users.Users.AddRange(await _ds.FrpUserService.GetApiUsersAsync());
            return FrpResponse.Create(users);
        }

        [Authorize]
        public override async Task<FrpResponse> AddUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.AddUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.ChangeUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.DeleteUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeUserPassword(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.ChangeUserPasswordAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> AddApiUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.AddApiUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeApiUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.ChangeApiUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteApiUser(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.DeleteApiUserAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeApiUserToken(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.ChangeApiUserTokenAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> GetApiUserToken(FrpUser request, ServerCallContext context)
        {
            return await _ds.FrpUserService.GetApiUserTokenAsync(request, _auth);
        }
    }
}
