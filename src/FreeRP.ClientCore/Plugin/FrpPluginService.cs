using FreeRP.ClientCore.Auth;
using FreeRP.FrpServices;
using FreeRP.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.ClientCore.Plugin
{
    public class FrpPluginService : IFrpPluginService, IAsyncDisposable
    {
        private readonly FrpAuthService _auth;
        

        public FrpPluginService(FrpAuthService auth)
        {
            _auth = auth;
            _auth.Connected += Server_Connected;
            _auth.Disconnected += Server_Disconnected;
        }

        private void Server_Connected(object? sender, IFrpAuthService e)
        {
            //_grpcClient = new(_auth.Channel);
        }

        private void Server_Disconnected(object? sender, IFrpAuthService e)
        {
            //_grpcClient = null;
        }

        public ValueTask DisposeAsync()
        {
            _auth.Connected -= Server_Connected;
            _auth.Disconnected -= Server_Disconnected;

            return ValueTask.CompletedTask;
        }

        public ValueTask<FrpResponse> AddPluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            throw new NotImplementedException();
        }

        public ValueTask<FrpResponse> ChangePluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            throw new NotImplementedException();
        }

        public ValueTask<FrpResponse> DeletePluginAsync(FrpPlugin plugin, IFrpAuthService auth)
        {
            throw new NotImplementedException();
        }

        

        public ValueTask<IEnumerable<FrpPlugin>> GetAllPluginsAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask<IEnumerable<FrpPlugin>> GetAllUserPluginsAsync(IFrpAuthService auth)
        {
            throw new NotImplementedException();
        }

        
    }
}
