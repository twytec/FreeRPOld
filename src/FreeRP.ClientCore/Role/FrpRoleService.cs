using FreeRP.ClientCore.Auth;
using FreeRP.FrpServices;
using FreeRP.Log;
using FreeRP.Role;

namespace FreeRP.ClientCore.Role
{
    public sealed class FrpRoleService : IFrpRoleService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcRoleService.GrpcRoleServiceClient? _grpcClient;

        public FrpRoleService(FrpAuthService auth)
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

        public async ValueTask<bool> IsRoleByIdExistsAsync(string id)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.IsRoleByIdExistsAsync(new() { Val = id }, await _auth.GetHeaderAsync());
                return res.BoolValue;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<bool> IsRoleByNameExistsAsync(string name)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.IsRoleByNameExistsAsync(new() { Val = name }, await _auth.GetHeaderAsync());
                return res.BoolValue;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<bool> IsUserInRoleAsync(string userId, string roleId)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.IsUserInRoleAsync(new() { RoleId = roleId, UserId = userId }, await _auth.GetHeaderAsync());
                return res.BoolValue;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpRole?> GetRoleByIdAsync(string id)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetRoleByIdAsync(new() { Val = id }, await _auth.GetHeaderAsync());
                if (res.ErrorType == FrpErrorType.ErrorNone)
                    return res.AnyData.Unpack<FrpRole>();
                else
                    return null;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpRole>> GetAllRolesAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetAllRolesAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpRoles>().Roles;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpUserInRole>> GetAllUserInRolesAsync()
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetAllUserInRolesAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpUserInRoles>().UserInRoles;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<IEnumerable<FrpRole>> GetUserRolesAsync(string userId)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetUserRolesAsync(new(), await _auth.GetHeaderAsync());
                return res.AnyData.Unpack<FrpRoles>().Roles;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.AddRoleAsync(role, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ChangeRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ChangeRoleAsync(role);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteRoleAsync(FrpRole role, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteRoleAsync(role);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetRoleAsync(FrpLog record, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ResetRoleAsync(record);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> AddUserToRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.AddUserToRoleAsync(userInRole);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteUserFromRoleAsync(FrpUserInRole userInRole, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteUserFromRoleAsync(userInRole);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> ResetUserInRoleAsync(FrpLog log, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.ResetUserInRoleAsync(log);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
