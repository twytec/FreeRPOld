using FreeRP.Auth;
using FreeRP.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpPluginService
    {
        public const string RecordTypePlugin = "Plugin";
        public const string RecordTypeMemberUsePlugin = "MemberUsePlugin";
        public const string UriSchemePlugin = "plugin";

        /// <summary>
        /// Returns all plugins
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpPlugin>> GetAllPluginsAsync();

        /// <summary>
        /// Returns all user plugins
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpPlugin>> GetAllUserPluginsAsync(IFrpAuthService auth);

        /// <summary>
        /// Add plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="auth"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> AddPluginAsync(FrpPlugin plugin, IFrpAuthService auth);

        /// <summary>
        /// Change plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="auth"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ChangePluginAsync(FrpPlugin plugin, IFrpAuthService auth);

        /// <summary>
        /// Delete plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="auth"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeletePluginAsync(FrpPlugin plugin, IFrpAuthService auth);
    }
}
