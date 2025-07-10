using FreeRP.ClientCore.Auth;
using FreeRP.Content;
using FreeRP.FrpServices;

namespace FreeRP.ClientCore.Content
{
    public sealed class FrpContentService : IFrpContentService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        private GrpcContentService.GrpcContentServiceClient? _grpcClient;

        public FrpContentService(FrpAuthService auth)
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

        public async ValueTask<FrpResponse> GetContentItemsAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.GetContentItemsAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DownloadAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DownloadAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> CreateDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.CreateDirectoryAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> MoveDirectoryAsync(FrpMoveContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.MoveDirectoryAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteDirectoryAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteDirectoryAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpContentStream> CreateFileAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.CreateFileAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpContentStream> FileStreamWriteAsync(FrpContentStream request)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.FileStreamWriteAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpContentStream> OpenFileReadAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.OpenFileReadAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpContentStream> FileStreamReadAsync(FrpContentStream request)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.FileStreamReadAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> MoveFileAsync(FrpMoveContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.MoveFileAsyncAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpResponse> DeleteFileAsync(FrpContentUriRequest request, IFrpAuthService authService)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.DeleteFileAsync(request, await _auth.GetHeaderAsync());
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, _auth.I18n.Text.ErrorNoConnectToServer);
        }
    }
}
