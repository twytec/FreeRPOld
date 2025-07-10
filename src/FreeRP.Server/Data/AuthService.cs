using FreeRP.ServerCore;
using FreeRP.ServerCore.Auth;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using FreeRP.User;

namespace FreeRP.Server.Data
{
    internal class AuthService(FrpSettings _frpSettings, FrpServices.IFrpDataService _ds, Localization.FrpLocalizationService _i18n)
    {
        public bool IsAdmin { get; set; }
        public event EventHandler<EventArgs>? OnLogin;
        public FrpServices.IFrpAuthService FrpAuthService { get; set; } = default!;

        public async Task<bool> LoginAdminAsync(FrpUser user)
        {
            if (_frpSettings.Admin.Email == user.Email && _frpSettings.Admin.Password == user.Password)
            {
                IsAdmin = true;
                
                user.Password = "";
                user.UserId = _frpSettings.Admin.UserId;
                user.Theme = _frpSettings.Admin.Theme;
                user.Language = _frpSettings.Admin.Language;

                var auth = new FrpAuthService(_ds, _frpSettings, _i18n) { IsAdmin = true };
                await auth.SetUserAsync(user);
                FrpAuthService = auth;

                OnLogin?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
    }
}
