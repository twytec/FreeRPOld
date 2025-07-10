using FreeRP.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpSettingsService
    {
        public const string FreeRPName = "myFreeRP";
        public const string SettingsFilename = "frpsettings.json";

        public const string DefaultDataFolderName = "frpdata";
        public const string TempFolderName = "temp";
        public const string ContentFolderName = "root";
        public const string BinFolderName = "bin";

        public static readonly string[] SystemRecordTypes = 
            [
                IFrpAuthService.RecordTypeAuth,
                IFrpConnectService.RecordTypeConnect,
                IFrpContentService.RecordTypeContent,
                IFrpDatabaseAccessService.RecordTypeDatabaseAccess,
                IFrpDatabaseService.RecordTypeDatabase,
                IFrpPermissionService.RecordTypePermission,
                IFrpRoleService.RecordTypeRole,
                IFrpRoleService.RecordTypeUserInRole,
                IFrpUserService.RecordTypeUser,
            ];

        ValueTask<FrpSettings> GetSettingsAsync(string path);
        ValueTask<bool> SaveSettingsAsync(string path, FrpSettings settings);
    }
}
