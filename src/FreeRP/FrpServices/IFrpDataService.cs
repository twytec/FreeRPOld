using FreeRP.User;

namespace FreeRP.FrpServices
{
    public interface IFrpDataService
    {
        /// <summary>
        /// FreeRP auth service
        /// Only on client
        /// </summary>
        IFrpAuthService FrpAuthService { get; set; }

        /// <summary>
        /// FreeRP permission service
        /// </summary>
        IFrpPermissionService FrpPermissionService { get; set; }

        /// <summary>
        /// FreeRP content service
        /// </summary>
        IFrpContentService FrpContentService { get; set; }

        /// <summary>
        /// FreeRP database service
        /// </summary>
        IFrpDatabaseService FrpDatabaseService { get; set; }

        /// <summary>
        /// FreeRP database unit service
        /// </summary>
        IFrpDatabaseAccessService FrpDatabaseAccessService { get; set; }

        /// <summary>
        /// FreeRP role service
        /// </summary>
        IFrpRoleService FrpRoleService { get; set; }

        /// <summary>
        /// FreeRP user service
        /// </summary>
        IFrpUserService FrpUserService { get; set; }

        /// <summary>
        /// FreeRP log service
        /// </summary>
        IFrpLogService FrpLogService { get; set; }

        /// <summary>
        /// FreeRP plugin service
        /// </summary>
        IFrpPluginService FrpPluginService { get; set; }
    }
}
