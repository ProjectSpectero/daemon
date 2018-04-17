using Newtonsoft.Json;

namespace Spectero.daemon.Models.Requests
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
    }
}