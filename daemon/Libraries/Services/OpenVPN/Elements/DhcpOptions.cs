using Newtonsoft.Json;
using Spectero.daemon.Libraries.Core.Deserialization;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    [JsonConverter(typeof(TolerantEnumConverter))]
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