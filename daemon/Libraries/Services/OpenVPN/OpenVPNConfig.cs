using System;
using System.Collections.Generic;
using System.Net;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPNConfig : IServiceConfig
    {
        internal List<Tuple<string, int, TransportProtocols>> listeners;
        internal IPNetwork localSubnet;
        internal List<IPNetwork> pushedNetworks;
        internal RedirectGatewayOptions redirectGateway;

        public OpenVPNConfig()
        {
            
        }
    }
}