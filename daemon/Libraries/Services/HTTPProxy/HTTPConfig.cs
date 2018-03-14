using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        public List<Tuple<string, int>> listeners { get; set; }
        public List<string> allowedDomains { get; }
        public List<string> bannedDomains { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HTTPProxyModes proxyMode { get; }

        public async Task<string> GetStringConfig()
        {
            return ToString();
        }
    }
}