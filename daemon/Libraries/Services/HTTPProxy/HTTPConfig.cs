using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public enum HTTPProxyModes
    {
        NORMAL,
        EXCLUSIVE_ALLOW
    }

    public class HTTPConfig : IServiceConfig
    {
        private Dictionary<IPAddress, int> listeners { get; }
        private List<String> allowedDomains { get; }
        private List<String> bannedDomains { get; }
        public HTTPProxyModes proxyMode { get; }

        public HTTPConfig (Dictionary<IPAddress, int> listeners, HTTPProxyModes proxyMode, List<String> allowedDomains = null, List<String> bannedDomains = null)
        {
            this.listeners = listeners;
            this.proxyMode = proxyMode;
            this.allowedDomains = allowedDomains;
            this.bannedDomains = bannedDomains;
        }
    }
}
