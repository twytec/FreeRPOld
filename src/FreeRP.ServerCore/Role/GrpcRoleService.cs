using FreeRP.FrpServices;
using FreeRP.Log;
using FreeRP.Role;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Role
{
    public class GrpcRoleService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.Role.GrpcRoleService.GrpcRoleServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> IsRoleByIdExists(FrpStringValueRequest request, ServerCallContext context)
        {
            return FrpResponse.CreateBool(await _ds.FrpRoleService.IsRoleByIdExistsAsync(request.Val));
        }

        [Authorize]
        public override async Task<FrpResponse> IsRoleByNameExists(FrpStringValueRequest request, ServerCallContext context)
        {
            return FrpResponse.CreateBool(await _ds.FrpRoleService.IsRoleByNameExistsAsync(request.Val));
        }

        [Authorize]
        public override async Task<FrpResponse> IsUserInRole(FrpUserInRole request, ServerCallContext context)
        {
            return FrpResponse.CreateBool(await _ds.FrpRoleService.IsUserInRoleAsync(request.UserId, request.RoleId));
        }

        [Authorize]
        public override async Task<FrpResponse> GetRoleById(FrpStringValueRequest request, ServerCallContext context)
        {
            if (await _ds.FrpRoleService.GetRoleByIdAsync(request.Val) is FrpRole r)
                return FrpResponse.Create(r);

            return FrpResponse.Create(FrpErrorType.ErrorRoleNotExist, _auth.I18n);
        }

        [Authorize]
        public override async Task<FrpResponse> GetAllRoles(Empty request, ServerCallContext context)
        {
            FrpRoles frpRoles = new();
            frpRoles.Roles.Add(await _ds.FrpRoleService.GetAllRolesAsync());
            return FrpResponse.Create(frpRoles);
        }

        [Authorize]
        public override async Task<FrpResponse> GetAllUserInRoles(Empty request, ServerCallContext context)
        {
            FrpUserInRoles userInRoles = new();
            userInRoles.UserInRoles.AddRange(await _ds.FrpRoleService.GetAllUserInRolesAsync());
            return FrpResponse.Create(userInRoles);
        }

        [Authorize]
        public override async Task<FrpResponse> GetUserRoles(FrpStringValueRequest request, ServerCallContext context)
        {
            FrpRoles frpRoles = new();
            frpRoles.Roles.Add(await _ds.FrpRoleService.GetUserRolesAsync(request.Val));
            return FrpResponse.Create(frpRoles);
        }

        [Authorize]
        public override async Task<FrpResponse> AddRole(FrpRole request, ServerCallContext context)
        {
            return await _ds.FrpRoleService.AddRoleAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeRole(FrpRole request, ServerCallContext context)
        {
            return await _ds.FrpRoleService.ChangeRoleAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteRole(FrpRole request, ServerCallContext context)
        {
            return await _ds.FrpRoleService.DeleteRoleAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> AddUserToRole(FrpUserInRole request, ServerCallContext context)
        {
            return await _ds.FrpRoleService.AddUserToRoleAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteUserFromRole(FrpUserInRole request, ServerCallContext context)
        {
            return await _ds.FrpRoleService.DeleteUserFromRoleAsync(request, _auth);
        }
    }
}
