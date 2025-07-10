using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers;
using FreeRP.ServerCore;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.ServerGrpc
{
    public class DatabaseService(FrpSettings frpSettings, IFrpDataService appData, IFrpAuthService authService) : Database.DatabaseService.DatabaseServiceBase
    {
        private readonly FrpSettings _frpSettings = frpSettings;
        private readonly IFrpDataService _appData = appData;
        private readonly IFrpAuthService _authService = authService;

        [Authorize]
        public override async Task<Response> DatabaseOpen(FrpDatabase request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.OpenDatabaseAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseItemAdd(DataRequest request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.AddItemAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseItemUpdate(DataRequest request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.ChangeItemAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseItemRemove(DataRequest request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.DeleteItemAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseItemQuery(QueryRequest request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.ItemQueryAsync(request, _authService);
        }

        [Authorize]
        public override Task<Response> DatabaseGet(DataRequest request, ServerCallContext context)
        {
            var db = _appData.FrpDatabaseService.GetDatabaseById(request.DatabaseId, _authService);
            if (db is not null)
            {
                if (db.Change is AccessValue.Allow || db.Create is AccessValue.Allow || db.Delete is AccessValue.Allow || db.Read is AccessValue.Allow)
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone, Data = GrpcJson.GetJson(db) });
                else
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
            }
            else
            {
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorDatabaseNotExist });
            }
        }

        [Authorize]
        public override async Task<Response> DatabaseCreate(FrpDatabase request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.AddDatabaseAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseChange(FrpDatabase request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.ChangeDatabaseAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DatabaseDelete(FrpDatabase request, ServerCallContext context)
        {
            return await _appData.FrpDatabaseService.DeleteDatabaseAsync(request, _authService);
        }
    }
}
