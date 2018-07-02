using Newtonsoft.Json;
using Spectero.daemon.Libraries.Core.Deserialization;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum RedirectGatewayOptions
    {
        Local,
        Def1,
        BypassDhcp
    }
}