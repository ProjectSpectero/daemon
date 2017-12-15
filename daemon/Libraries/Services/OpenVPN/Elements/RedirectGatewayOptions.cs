using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RedirectGatewayOptions
    {
        Local,
        Def1,
        BypassDhcp
    }
}