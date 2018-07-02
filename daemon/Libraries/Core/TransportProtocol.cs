using Newtonsoft.Json;
using Spectero.daemon.Libraries.Core.Deserialization;

namespace Spectero.daemon.Libraries.Core
{
    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TransportProtocol
    {
        TCP,
        UDP
    }
}