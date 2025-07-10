using FreeRP.ClientCore.Auth;
using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Log;

namespace FreeRP.ClientCore.Database
{
    public sealed class FrpDatabaseAccessService : IFrpDatabaseAccessService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcDatabaseAccessService.GrpcDatabaseAccessServiceClient? _grpcClient;

        public FrpDatabaseAccessService(FrpAuthService auth)
        {
            _auth = auth;
            _auth.Connected += Server_Connected;
            _auth.Disconnected += Server_Disconnected;
        }

        public ValueTask DisposeAsync()
        {
            _auth.Connected -= Server_Connected;
            _auth.Disconnected -= Server_Disconnected;

            return ValueTask.CompletedTask;
        }

        private void Server_Connected(object? sender, IFrpAuthService e)
        {
            _grpcClient = new(_auth.Channel);
        }

        private void Server_Disconnected(object? sender, IFrpAuthService e)
        {
            _grpcClient = null;
        }

        public async ValueTask<FrpDatabasePermissions> GetDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetDatabasePermissionsAsync(new() { Val = db.DatabaseId }, await _auth.GetHeaderAsync());
                if (res.ErrorType is FrpErrorType.ErrorNone)
                {
                    var p = Helpers.Json.GetModel<FrpDatabasePermissions>(res.Data);
                    if (p is not null)
                        return p;
                }

                return new(db);
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> OpenDatabaseAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.OpenDatabaseAsync(new() { Val = frpDatabaseId }, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> SaveChangesAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.SaveChangesAsync(new() { Val = frpDatabaseId }, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> CloseDatabaseAsync(string frpDatabaseId, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.CloseDatabaseAsync(new() { Val = frpDatabaseId }, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.AddDatasetAsync(dataRequest, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ChangeDatasetAsync(dataRequest, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteDatasetAsync(FrpDataRequest dataRequest, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteDatasetAsync(dataRequest, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> FirstOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.FirstOrDefaultAsync(queryRequest, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ListOrDefaultAsync(FrpQueryRequest queryRequest, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ListOrDefaultAsync(queryRequest, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetDatabaseAccessAsync(FrpLog log, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ResetDatabaseAccessAsync(log, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
