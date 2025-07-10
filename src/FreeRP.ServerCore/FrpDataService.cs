using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.ServerCore.Content;
using FreeRP.ServerCore.Database;
using FreeRP.ServerCore.Log;
using FreeRP.ServerCore.Permission;
using FreeRP.ServerCore.Plugin;
using FreeRP.ServerCore.Role;
using FreeRP.ServerCore.User;
using FreeRP.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace FreeRP.ServerCore
{
    public sealed class FrpDataService : IFrpDataService, IAsyncDisposable
    {
        public IFrpPermissionService FrpPermissionService { get; set; }
        public IFrpContentService FrpContentService { get; set; }
        public IFrpDatabaseService FrpDatabaseService { get; set; }
        public IFrpDatabaseAccessService FrpDatabaseAccessService { get; set; }
        public IFrpRoleService FrpRoleService { get; set; }
        public IFrpUserService FrpUserService { get; set; }
        public IFrpLogService FrpLogService { get; set; }
        public IFrpPluginService FrpPluginService { get; set; }
        public IFrpAuthService FrpAuthService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private readonly FrpSettings _frpSettings;
        private readonly FrpDatabase _db;

        public FrpDataService(IWebHostEnvironment env, FrpSettings frpSettings, ILogger<FrpDataService> logger)
        {
            _frpSettings = frpSettings;
            _db = new()
            {
                DatabaseId = frpSettings.DatabaseSettings.DatabaseId,
                DatabaseProvider = frpSettings.DatabaseSettings.DatabaseProvider
            };

            FrpLogService = new FrpLogService(this, frpSettings);
            FrpRoleService = new FrpRoleService(this, frpSettings, FrpLogService, _db, logger);
            FrpUserService = new FrpUserService(this, frpSettings, FrpLogService, _db, logger);
            FrpPermissionService = new FrpPermissionService(this, frpSettings, FrpLogService, _db, logger);
            FrpPluginService = new FrpPluginService(this, frpSettings, FrpLogService, _db, logger);

            FrpContentService = new FrpContentService(env, this, FrpLogService, logger, frpSettings);
            FrpDatabaseService = new FrpDatabaseService(this, frpSettings, FrpLogService, _db, logger);
            FrpDatabaseAccessService = new FrpDatabaseAccessService(this, FrpLogService, frpSettings, logger);
        }

        public async ValueTask InitAsync()
        {
            IFrpRecordContext con = FrpRecordContextFactory.Create(_frpSettings, _db);
            await con.CreateDatabaseAsync();
            await con.CloseDatabaseAsync();

            await (FrpDatabaseService as FrpDatabaseService)!.InitAsync();
            await (FrpPermissionService as FrpPermissionService)!.InitAsync();
            await (FrpRoleService as FrpRoleService)!.InitAsync();
            await (FrpUserService as FrpUserService)!.InitAsync();
            await (FrpPluginService as FrpPluginService)!.InitAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await FrpLogService.DisposeAsync();
        }
    }
}
