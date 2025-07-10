using FreeRP.Auth;
using FreeRP.Localization;
using FreeRP.User;

namespace FreeRP.FrpServices
{
    public interface IFrpConnectService
    {
        public const string RecordTypeConnect = "FrpConnect";

        FrpConnectResponse FrpConnectResponse { get; }

        /// <summary>
        /// Try connect to server.
        /// This API is only supported on client
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        ValueTask<bool> TryConnectAsync(string host, FrpLocalization i18n);

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpConnectResponse> HelloAsync(FrpLocalization i18n);

        /// <summary>
        /// Log in to the server with e-mail and password
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        ValueTask<FrpLoginResponse> LoginAsync(FrpUser user, FrpLocalization i18n);

        /// <summary>
        /// Check connection to the server and user authorisation
        /// </summary>
        /// <param name="pingData"></param>
        /// <returns></returns>
        ValueTask<FrpPingData> PingServerAsync(FrpPingData pingData);
    }
}
