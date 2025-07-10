using FreeRP.ClientCore.Auth;
using FreeRP.ClientCore.Content;
using FreeRP.ClientCore.Database;
using FreeRP.ClientCore.Log;
using FreeRP.ClientCore.Permission;
using FreeRP.ClientCore.Plugin;
using FreeRP.ClientCore.Role;
using FreeRP.ClientCore.User;
using FreeRP.FrpServices;
using FreeRP.Localization;

namespace FreeRP.ClientCore
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
        public IFrpAuthService FrpAuthService { get; set; }
        public IFrpPluginService FrpPluginService { get; set; }

        public FrpDataService(FrpClientSettings clientSettings, FrpLocalizationService i18n)
        {
            FrpAuthService auth = new(clientSettings, i18n);
            FrpAuthService = auth;

            FrpPermissionService = new FrpPermissionService(auth);
            FrpContentService = new FrpContentService(auth);
            FrpDatabaseService = new FrpDatabaseService(auth);
            FrpDatabaseAccessService = new FrpDatabaseAccessService(auth);
            FrpRoleService = new FrpRoleService(auth);
            FrpUserService = new FrpUserService(auth);
            FrpLogService = new FrpLogService(auth);
            FrpPluginService = new FrpPluginService(auth);
        }

        public async ValueTask DisposeAsync()
        {
            if (FrpAuthService is FrpAuthService a) await a.DisposeAsync();
            if (FrpContentService is FrpContentService b) await b.DisposeAsync();
            if (FrpDatabaseAccessService is FrpDatabaseAccessService c) await c.DisposeAsync();
            if (FrpDatabaseService is FrpDatabaseService d) await d.DisposeAsync();
            if (FrpLogService is FrpLogService e) await e.DisposeAsync();
            if (FrpPermissionService is FrpPermissionService f) await f.DisposeAsync();
            if (FrpRoleService is FrpRoleService g) await g.DisposeAsync();
            if (FrpUserService is FrpUserService h) await h.DisposeAsync();
            if (FrpPluginService is FrpPluginService p) await p.DisposeAsync();
        }
    }
}
