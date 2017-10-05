﻿using System;
using System.Collections.Generic;
using System.Net;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPConfig : IServiceConfig
    {
        internal Dictionary<IPAddress, int> listeners { get; }
        internal List<String> allowedDomains { get; }
        internal List<String> bannedDomains { get; }
        internal HTTPProxyModes proxyMode { get; }

        public HTTPConfig (Dictionary<IPAddress, int> listeners, HTTPProxyModes proxyMode, List<String> allowedDomains = null, List<String> bannedDomains = null)
        {
            this.listeners = listeners;
            this.proxyMode = proxyMode;
            this.allowedDomains = allowedDomains;
            this.bannedDomains = bannedDomains;
        }
    }
}
