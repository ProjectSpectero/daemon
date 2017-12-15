using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DhcpOptions
    {
        Domain,
        Dns,
        Wins,
        Nbdd,
        Ntp,
        Nbt,
        NbsScopeId,
        DisableNbt
    }
}