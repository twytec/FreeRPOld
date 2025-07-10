using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Log;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Database
{
    public class GrpcDatabaseAccessService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.Database.GrpcDatabaseAccessService.GrpcDatabaseAccessServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> GetDatabasePermissions(FrpStringValueRequest request, ServerCallContext context)
        {
            var db = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(request.Val);
            if (db is not null)
            {
                var res = await _ds.FrpDatabaseAccessService.GetDatabasePermissionsAsync(db, _auth);
                return FrpResponse.Create(Helpers.Json.GetJson(res));
            }

            return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, _auth.I18n);
        }

        [Authorize]
        public override async Task<FrpResponse> OpenDatabase(FrpStringValueRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.OpenDatabaseAsync(request.Val, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> SaveChanges(FrpStringValueRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.SaveChangesAsync(request.Val, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> CloseDatabase(FrpStringValueRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.CloseDatabaseAsync(request.Val, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> AddDataset(FrpDataRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.AddDatasetAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeDataset(FrpDataRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.ChangeDatasetAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteDataset(FrpDataRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.DeleteDatasetAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> FirstOrDefault(FrpQueryRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.FirstOrDefaultAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ListOrDefault(FrpQueryRequest request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseAccessService.ListOrDefaultAsync(request, _auth);
        }
    }
}
