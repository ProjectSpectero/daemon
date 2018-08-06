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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Spectero.daemon.Models.Opaque.Requests
{
    public class UnauthNodeAddRequest
    {
        [JsonProperty("node_key")]
        public string NodeKey { get; set; }
        [JsonProperty("ip")]
        public string Ip { get; set; }
        [JsonProperty("port")]
        public int Port { get; set; }
        [JsonProperty("protocol")]
        public string Protocol { get; set; }
        [JsonProperty("install_id")]
        public string InstallId { get; set; }
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("system_data")]
        public Dictionary<string, object> SystemData { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}