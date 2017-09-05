using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPProxy : IService
    {
        private HTTPConfig proxyConfig;
        private ProxyServer proxyServer = new ProxyServer();

        public void Start(IServiceConfig serviceConfig)
        {
            this.proxyConfig = (HTTPConfig) serviceConfig;
            
            
            //Loop through and listen on all defined IP <-> port pairs
            foreach (KeyValuePair<IPAddress, int> listener in proxyConfig.listeners)
            {
                proxyServer.AddEndPoint(new ExplicitProxyEndPoint(listener.Key, listener.Value, false));
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Dictionary<String, String> getStatistics ()
        {
            throw new NotImplementedException();
        }
    }
}
