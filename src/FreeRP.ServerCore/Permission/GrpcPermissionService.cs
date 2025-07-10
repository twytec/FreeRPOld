using FreeRP.FrpServices;
using FreeRP.Log;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Permission
{
    public class GrpcPermissionService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.GrpcPermissionService.GrpcPermissionServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> GetPermissionById(FrpStringValueRequest request, ServerCallContext context)
        {
            var p = await _ds.FrpPermissionService.GetPermissionByIdAsync(request.Val);
            if (p is null)
                return FrpResponse.Create(FrpErrorType.ErrorAccessNotExist, _auth.I18n);

            return FrpResponse.Create(p);
        }

        [Authorize]
        public override async Task<FrpResponse> GetUserContentPermission(FrpUri request, ServerCallContext context)
        {
            var p = await _ds.FrpPermissionService.GetUserContentPermissionAsync(request, _auth);
            return FrpResponse.Create(p);
        }

        [Authorize]
        public override async Task<FrpResponse> GetUserDatabasePermissions(FrpStringValueRequest request, ServerCallContext context)
        {
            FrpPermissions res = new();

            var db = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(request.Val);
            if (db is not null)
            {
                var p = await _ds.FrpPermissionService.GetUserDatabasePermissionsAsync(db, _auth);
                res.Permissions.AddRange(p);
            }

            return FrpResponse.Create(res);
        }

        [Authorize]
        public override async Task<FrpResponse> GetDatabasePermissions(FrpStringValueRequest request, ServerCallContext context)
        {
            FrpPermissions res = new();
            var p = await _ds.FrpPermissionService.GetDatabasePermissionsAsync(request.Val);
            res.Permissions.AddRange(p);
            return FrpResponse.Create(res);
        }

        [Authorize]
        public override async Task<FrpResponse> GetContentPermissions(FrpStringValueRequest request, ServerCallContext context)
        {
            FrpPermissions res = new();
            var p = await _ds.FrpPermissionService.GetContentPermissionsAsync(request.Val);
            res.Permissions.AddRange(p);
            return FrpResponse.Create(res);
        }

        [Authorize]
        public override async Task<FrpResponse> AddPermission(FrpPermission request, ServerCallContext context)
        {
            return await _ds.FrpPermissionService.AddPermissionAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangePermission(FrpPermission request, ServerCallContext context)
        {
            return await _ds.FrpPermissionService.ChangePermissionAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeletePermission(FrpPermission request, ServerCallContext context)
        {
            return await _ds.FrpPermissionService.DeletePermissionAsync(request, _auth);
        }
    }
}
