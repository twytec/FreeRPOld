using FreeRP.FrpServices;
using FreeRP.Settings;
using Grpc.Core;

namespace FreeRP.ServerCore.Settings
{
    public class GrpcSettingsService(FrpSettings frpSettings, IFrpAuthService auth) : GrpcSettingService.GrpcSettingServiceBase
    {
        private readonly FrpSettings _frpSettings = frpSettings;
        private readonly IFrpAuthService _auth = auth;

        public override Task<FrpResponse> GetSettings(Empty request, ServerCallContext context)
        {
            if (_auth.IsAdmin)
            {
                return Task.FromResult(FrpResponse.Create(_frpSettings));
            }

            return Task.FromResult(FrpResponse.Create(FrpErrorType.ErrorAccessDenied, _auth.I18n));
        }
    }
}
