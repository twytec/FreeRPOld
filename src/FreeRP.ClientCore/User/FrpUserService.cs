using FreeRP.ClientCore.Auth;
using FreeRP.FrpServices;
using FreeRP.Log;
using FreeRP.User;

namespace FreeRP.ClientCore.User
{
    public sealed class FrpUserService : IFrpUserService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcUserService.GrpcUserServiceClient? _grpcClient;

        public FrpUserService(FrpAuthService auth)
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

        public async ValueTask<FrpUser?> GetUserByEmailAsync(string name)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserByEmailAsync(new() { Val = name }, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUser>();
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpUser?> GetUserByIdAsync(string id)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserByIdAsync(new() { Val = id }, await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUser>();
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<bool> IsUserByIdExistsAsync(string id)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserByIdAsync(new() { Val = id }, await _auth.GetHeaderAsync());
                return res.BoolValue;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpUser>> GetAllUsersAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetAllUsersAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUsers>().Users;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpUser>> GetUsersAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUsersAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUsers>().Users;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpUser>> GetApiUsersAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetApiUsersAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUsers>().Users;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddUserAsync(FrpUser u, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.AddUserAsync(u, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeUserAsync(FrpUser user, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.ChangeUserAsync(user, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteUserAsync(FrpUser u, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.DeleteUserAsync(u, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeUserPasswordAsync(FrpUser user, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.DeleteUserAsync(user, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddApiUserAsync(FrpUser u, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.AddApiUserAsync(u, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeApiUserAsync(FrpUser user, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.ChangeApiUserAsync(user, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteApiUserAsync(FrpUser u, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.DeleteApiUserAsync(u, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeApiUserTokenAsync(FrpUser user, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.ChangeApiUserTokenAsync(user, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> GetApiUserTokenAsync(FrpUser user, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.GetApiUserTokenAsync(user, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetUserAsync(FrpLog log, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.ResetUserAsync(log, await _auth.GetHeaderAsync());
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
