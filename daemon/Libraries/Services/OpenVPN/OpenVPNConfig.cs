using System;
using System.Collections.Generic;
using System.Net;
using Spectero.daemon.Libraries.Core;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPNConfig : IServiceConfig
    {
        internal List<Tuple<string, int, TransportProtocols>> listeners;
        internal IPNetwork localSubnet;
        internal List<IPNetwork> pushedNetworks;
        internal bool redirectGateway;

        public OpenVPNConfig()
        {
            
        }
    }
}