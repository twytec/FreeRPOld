using FreeRP.FrpServices;
using FreeRP.Localization;
using FreeRP.ServerCore.Auth;
using FreeRP.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.UnitTest
{
    internal static class MySettings
    {
        public static FrpSettings FrpSettings;
        public static FrpDataService FrpDataService;
        public static FrpAuthService TestAdmin;
        public static FrpLocalization I18n = new();

        static MySettings()
        {
            var old = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IFrpSettingsService.DefaultDataFolderName);
            if (Directory.Exists(old))
                Directory.Delete(old, true);

            Settings.FrpSettingsService frpSettingsService = new();
            FrpSettings = frpSettingsService.GetSettingsAsync(AppDomain.CurrentDomain.BaseDirectory).GetAwaiter().GetResult();

            FrpDataService = new FrpDataService(new MyWebHostEnvironment(), FrpSettings, new Microsoft.Extensions.Logging.Abstractions.NullLogger<FrpDataService>());
            FrpDataService.InitAsync().GetAwaiter().GetResult();

            TestAdmin = new(FrpDataService, FrpSettings, new()) { IsAdmin = true, User = FrpSettings.Admin };
        }
    }
}
