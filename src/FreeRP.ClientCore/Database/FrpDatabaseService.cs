using FreeRP.ClientCore.Auth;
using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Log;

namespace FreeRP.ClientCore.Database
{
    public sealed class FrpDatabaseService : IFrpDatabaseService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcDatabaseService.GrpcDatabaseServiceClient? _grpcClient;

        public FrpDatabaseService(FrpAuthService auth)
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

        public async ValueTask<IEnumerable<FrpDatabase>> GetAllDatabasesAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetAllDatabasesAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpDatabases>().Databases;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpDatabase?> GetDatabaseByIdAsync(string databaseId)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetDatabaseByIdAsync(new() { Val = databaseId }, await _auth.GetHeaderAsync());
                if (res.ErrorType == FrpErrorType.ErrorDatabaseNotExist)
                    return null;

                return res.AnyData.Unpack<FrpDatabase>();
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddDatabaseAsync(FrpDatabase database, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.AddDatabaseAsync(database, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ChangeDatabaseAsync(ndb, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteDatabaseAsync(FrpDatabase ndb, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteDatabaseAsync(ndb, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetDatabaseAsync(FrpLog record, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ResetDatabaseAsync(record, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
