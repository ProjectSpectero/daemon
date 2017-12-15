using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Spectero.daemon.Libraries.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TransportProtocols
    {
        TCP,
        UDP
    }
}