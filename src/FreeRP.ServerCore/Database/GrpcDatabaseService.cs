using FreeRP.Database;
using FreeRP.FrpServices;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerCore.Database
{
    public class GrpcDatabaseService(IFrpDataService ds, IFrpAuthService auth) : FreeRP.Database.GrpcDatabaseService.GrpcDatabaseServiceBase
    {
        private readonly IFrpDataService _ds = ds;
        private readonly IFrpAuthService _auth = auth;

        [Authorize]
        public override async Task<FrpResponse> GetAllDatabases(Empty request, ServerCallContext context)
        {
            FrpDatabases res = new();
            var d = await _ds.FrpDatabaseService.GetAllDatabasesAsync();
            res.Databases.AddRange(d);
            return FrpResponse.Create(res);
        }

        [Authorize]
        public override async Task<FrpResponse> GetDatabaseById(FrpStringValueRequest request, ServerCallContext context)
        {
            var d = await _ds.FrpDatabaseService.GetDatabaseByIdAsync(request.Val);
            if (d is not null)
                return FrpResponse.Create(d);

            return FrpResponse.Create(FrpErrorType.ErrorDatabaseNotExist, _auth.I18n);
        }

        [Authorize]
        public override async Task<FrpResponse> AddDatabase(FrpDatabase request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseService.AddDatabaseAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> ChangeDatabase(FrpDatabase request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseService.ChangeDatabaseAsync(request, _auth);
        }

        [Authorize]
        public override async Task<FrpResponse> DeleteDatabase(FrpDatabase request, ServerCallContext context)
        {
            return await _ds.FrpDatabaseService.DeleteDatabaseAsync(request, _auth);
        }
    }
}
