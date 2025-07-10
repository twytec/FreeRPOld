using FreeRP.Auth;
using FreeRP.Localization;
using FreeRP.Plugins;
using FreeRP.Role;
using FreeRP.User;
using Grpc.Net.Client;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FreeRP.FrpServices
{
    public interface IFrpAuthService
    {
        public const string RecordTypeAuth = "FrpAuth";

        FrpConnectResponse FrpConnectResponse { get; }
        FrpUser User { get; }
        List<FrpRole> Roles { get; }
        List<FrpPlugin> Plugins { get; }
        FrpLocalizationService I18n { get; }
        bool IsAdmin { get; }
        bool IsLogin { get; }

        string? CurrentServer { get; }
        event EventHandler<IFrpAuthService>? Connected;
        event EventHandler<IFrpAuthService>? Disconnected;

        /// <summary>
        /// Try connect to server.
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpConnectResponse> ConnectAsync(string host);

        /// <summary>
        /// Log in to the server with e-mail and password
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        ValueTask<FrpLoginResponse> LoginAsync(FrpUser user);

        /// <summary>
        /// Log in with token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<FrpLoginResponse> LoginWithTokenAsync(string token);

        /// <summary>
        /// Returns a token
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpToken> GetTokenAsync();

        /// <summary>
        /// Check connection to the server and user authorisation
        /// </summary>
        /// <param name="pingData"></param>
        /// <returns></returns>
        ValueTask<FrpPingData> PingServerAsync(FrpPingData pingData);
    }
}
