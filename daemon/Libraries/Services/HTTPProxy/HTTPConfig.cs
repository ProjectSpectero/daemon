/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
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