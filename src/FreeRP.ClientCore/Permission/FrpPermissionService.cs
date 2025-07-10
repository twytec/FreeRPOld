using FreeRP.ClientCore.Auth;
using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Log;

namespace FreeRP.ClientCore.Permission
{
    public sealed class FrpPermissionService : IFrpPermissionService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcPermissionService.GrpcPermissionServiceClient? _grpcClient;

        public FrpPermissionService(FrpAuthService auth)
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

        public async ValueTask<FrpPermission?> GetPermissionByIdAsync(string id)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetPermissionByIdAsync(new() { Val = id }, await _auth.GetHeaderAsync());
                if (res.ErrorType == FrpErrorType.ErrorAccessNotExist)
                    return null;

                return res.AnyData.Unpack<FrpPermission>();
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpPermission> GetUserContentPermissionAsync(FrpUri uri, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserContentPermissionAsync(uri, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpPermission>();
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpPermission>> GetUserDatabasePermissionsAsync(FrpDatabase db, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserDatabasePermissionsAsync(new() { Val = db.DatabaseId }, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpPermissions>().Permissions;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpPermission>> GetContentPermissionsAsync(string uri)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetContentPermissionsAsync(new() { Val = uri }, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpPermissions>().Permissions;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpPermission>> GetDatabasePermissionsAsync(string databaseId)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetDatabasePermissionsAsync(new() { Val = databaseId }, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpPermissions>().Permissions;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddPermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.AddPermissionAsync(ac, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangePermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await (_grpcClient.ChangePermissionAsync(ac, await _auth.GetHeaderAsync()));
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeletePermissionAsync(FrpPermission ac, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeletePermissionAsync(ac, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetPermissionAysnc(FrpLog log, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ResetPermissionAsync(log, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
