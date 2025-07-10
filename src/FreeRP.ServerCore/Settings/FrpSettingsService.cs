using FreeRP.Database;
using FreeRP.FrpServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ServerCore.Settings
{
    public class FrpSettingsService : IFrpSettingsService
    {
        public async ValueTask<FreeRP.Settings.FrpSettings> GetSettingsAsync(string path)
        {
            FreeRP.Settings.FrpSettings? settings = null;

            var file = Path.Combine(path, IFrpSettingsService.SettingsFilename);
            if (File.Exists(file))
            {
                var json = await File.ReadAllTextAsync(file);
                Helpers.GrpcJson.TryGetModel<FreeRP.Settings.FrpSettings>(json, out settings);
            }

            if (settings is null)
            {
                settings = new() {
                    ServerName = "myFreeRP",
                    StaticPaths = false,
                    Admin = new()
                    {
                        UserId = "0",
                        Email = "Admin",
                        Password = "",
                        Language = "en",
                        Theme = new() { DarkMode = true, RightToLeft = false }
                    },
                    System = new()
                    {
                        UserId = "-1",
                        Email = "System",
                        Password = "",
                        Language = "en",
                        Theme = new() { DarkMode = true, RightToLeft = false }
                    },
                    LoginSettings = new()
                    {
                        Passwordless = false,
                        MinPasswordLength = 8,
                        PasswordSigningKey = Guid.NewGuid().ToString("N"),
                        TokenSigningKey = Guid.NewGuid().ToString("N"),
                        UseRefreshToken = true,
                        TokenValidityInHours = 24,
                        ShortTimeTokenValidityInMinutes = 3,
                    },
                    ContentSettings = new(),
                    DatabaseSettings = new() { DatabaseId = "freeRPDataDb", DatabaseProvider = DatabaseProvider.Sqlite },
                    GrpcSettings = new() { GrpcMessageSizeInByte = 4 * 1024 * 1024, GrpcTransportTimeoutInMinutes = 5 },
                    LogSettings = new() { DatabaseId = "freeRPLogDb", DatabaseProvider = DatabaseProvider.Sqlite, LogAll = true },
                    SmtpSettings = new()
                };

                settings.LogSettings.LogRecordTypes.AddRange(IFrpSettingsService.SystemRecordTypes);

                await SaveSettingsAsync(path, settings);
            }

            if (settings.StaticPaths == false)
            {
                var root = Path.Combine(path, IFrpSettingsService.DefaultDataFolderName);
                Helpers.FileSystem.CreateDirectory(root);

                settings.ContentSettings.BinRootPath = Path.Combine(root, IFrpSettingsService.BinFolderName);
                Helpers.FileSystem.CreateDirectory(settings.ContentSettings.BinRootPath);

                settings.ContentSettings.ContentRootPath = Path.Combine(root, IFrpSettingsService.ContentFolderName);
                Helpers.FileSystem.CreateDirectory(settings.ContentSettings.ContentRootPath);

                settings.ContentSettings.TempRootPath = Path.Combine(root, IFrpSettingsService.TempFolderName);
                Helpers.FileSystem.CreateDirectory(settings.ContentSettings.TempRootPath);

                settings.DatabaseSettings.DatabaseRootPath = Path.Combine(root, IFrpDatabaseService.DatabasesFolderName);
                Helpers.FileSystem.CreateDirectory(settings.DatabaseSettings.DatabaseRootPath);

                settings.LogSettings.LogRootPath = Path.Combine(root, IFrpLogService.LogFolderName);
                Helpers.FileSystem.CreateDirectory(settings.LogSettings.LogRootPath);
            }

            return settings;
        }

        public async ValueTask<bool> SaveSettingsAsync(string path, FreeRP.Settings.FrpSettings settings)
        {
            try
            {
                var json = Helpers.Json.GetJsonIndented(settings);
                var file = Path.Combine(path, IFrpSettingsService.SettingsFilename);
                await File.WriteAllTextAsync(file, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
