using System;
using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPConfig : IServiceConfig
    {
        public HTTPConfig(List<Tuple<string, int>> listeners, HTTPProxyModes proxyMode,
            List<string> allowedDomains = null, List<string> bannedDomains = null)
        {
            this.listeners = listeners;
            this.proxyMode = proxyMode;
            this.allowedDomains = allowedDomains;
            this.bannedDomains = bannedDomains;
        }

        internal List<Tuple<string, int>> listeners { get; }
        internal List<string> allowedDomains { get; }
        internal List<string> bannedDomains { get; }
        internal HTTPProxyModes proxyMode { get; }
    }
}