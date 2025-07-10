using FreeRP.Auth;
using FreeRP.FrpServices;
using FreeRP.Localization;
using FreeRP.Plugins;
using FreeRP.Role;
using FreeRP.User;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;

namespace FreeRP.ClientCore.Auth
{
    public sealed class FrpAuthService(FrpClientSettings settings, FrpLocalizationService i18n) : IFrpAuthService, IAsyncDisposable
    {
        public FrpConnectResponse FrpConnectResponse { get; private set; } = new();
        public FrpUser User { get; private set; } = new FrpUser();
        public List<FrpRole> Roles { get; } = [];
        public List<FrpPlugin> Plugins { get; } = [];
        public FrpLocalizationService I18n { get; private set; } = i18n;
        public bool IsAdmin { get; private set; }
        public bool IsLogin { get; private set; }

        internal GrpcChannel? Channel { get; private set; }

        private readonly FrpClientSettings _settings = settings;
        private GrpcAuthService.GrpcAuthServiceClient? _grpcClient;
        private readonly FrpToken _token = new();
        private readonly Grpc.Core.Metadata _longTimeHeader = [];

        public string? CurrentServer { get; private set; }
        public event EventHandler<IFrpAuthService>? Connected;
        public event EventHandler<IFrpAuthService>? Disconnected;

        public void OnConnected()
        {
            Connected?.Invoke(this, this);
        }
        public void OnDisconnected() => Disconnected?.Invoke(this, this);

        public async ValueTask<FrpConnectResponse> ConnectAsync(string host)
        {
            if (string.IsNullOrEmpty(host) == false)
                _settings.FrpServers.Add(host);

            foreach (var item in _settings.FrpServers)
            {
                await DisposeAsync();
                try
                {
                    if (OperatingSystem.IsBrowser())
                    {
                        Channel = GrpcChannel.ForAddress(item, new GrpcChannelOptions
                        {
                            HttpHandler = new GrpcWebHandler(new HttpClientHandler())
                        });
                    }
                    else
                    {
                        Channel = GrpcChannel.ForAddress(item);
                    }

                    _grpcClient = new(Channel);
                    var res = await _grpcClient.ConnectAsync(new FrpStringValueRequest() { Val = item });
                    FrpConnectResponse.MergeFrom(res);
                    CurrentServer = item;
                    OnConnected();
                    return res;
                }
                catch (Exception)
                {
                }
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, I18n.Text.ErrorNoConnectToServer);
        }

        #region Login

        public async ValueTask<FrpLoginResponse> LoginAsync(FrpUser user)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.LoginAsync(user);
                SetUser(res);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpLoginResponse> LoginWithTokenAsync(string token)
        {
            if (_grpcClient is not null)
            {
                var res = await _grpcClient.LoginWithTokenAsync(new() { Val = token });
                SetUser(res);
                return res;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, I18n.Text.ErrorNoConnectToServer);
        }

        private void SetUser(FrpLoginResponse res)
        {
            if (res.ErrorType == FrpErrorType.ErrorNone)
            {
                User.MergeFrom(res.User);

                Roles.Clear();
                Roles.AddRange(res.Roles);

                Plugins.Clear();
                Plugins.AddRange(res.Plugins);

                I18n.SetText(res.User.Language);

                IsAdmin = res.IsAdmin;
                IsLogin = true;

                _token.MergeFrom(res.Token);
                _longTimeHeader.Clear();
                _longTimeHeader.Add("Authorization", $"Bearer {res.Token.Token}");
            }
            else
            {
                throw FrpException.GetFrpException(res.ErrorType, res.ErrorMessage);
            }
        }

        #endregion

        public async ValueTask<FrpToken> GetTokenAsync()
        {
            if (_grpcClient is not null)
            {
                if (FrpConnectResponse.UseRefreshToken)
                {
                    return await _grpcClient.GetTokenAsync(new Empty(), _longTimeHeader);
                }
                else
                    return _token;
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, I18n.Text.ErrorNoConnectToServer);
        }

        public async ValueTask<FrpPingData> PingServerAsync(FrpPingData pingData)
        {
            if (_grpcClient is not null)
            {
                return await _grpcClient.PingServerAsync(pingData, _longTimeHeader);
            }

            throw FrpException.GetFrpException(FrpErrorType.ErrorNoConnectToServer, I18n.Text.ErrorNoConnectToServer);
        }

        internal async ValueTask<Grpc.Core.Metadata> GetHeaderAsync()
        {
            if (FrpConnectResponse.UseRefreshToken == false)
            {
                return _longTimeHeader;
            }

            var token = await GetTokenAsync();
            return new Grpc.Core.Metadata() { { "Authorization", $"Bearer {token.Token}" } };
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Channel is not null)
                {
                    await Channel.ShutdownAsync();
                    Channel.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
