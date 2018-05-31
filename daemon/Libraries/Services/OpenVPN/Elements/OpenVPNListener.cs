using Spectero.daemon.Libraries.Core;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    public class OpenVPNListener
    {
        // This *should* be modelled as an IPAddress, but that just incurs additional serialization/deserialization overhead.
        public string IPAddress;

        public int Port;
        public int ManagementPort;
        public TransportProtocols Protocol;

        // This *should* be modelled as an IPNetwork, but that just incurs additional serialization/deserialization overhead.
        public string Network;
    }
}