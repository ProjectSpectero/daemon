using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;

namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Defaults
    {
        public static readonly string[] ValidServices = { "HTTPProxy", "OpenVPN", "ShadowSOCKS", "SSHTunnel" };
        public static readonly string[] ValidActions = { "start", "stop", "restart", "config" };

        public static Lazy<HTTPConfig> HTTP
        {
            get
            {
                var listeners = new List<Tuple<string, int>>
                {
                    Tuple.Create(IPAddress.Any.ToString(), 8800)
                };
                return new Lazy<HTTPConfig>(new HTTPConfig(listeners, HTTPProxyModes.Normal));
            }
        }

        public static Lazy<OpenVPNConfig> OpenVPN
        {
            get
            {
                var config = new OpenVPNConfig(null, null)
                {
                    AllowMultipleConnectionsFromSameClient = false,
                    ClientToClient = false,
                    pushedNetworks = new List<IPNetwork>(),
                    redirectGateway = new List<RedirectGatewayOptions> {RedirectGatewayOptions.Def1},
                    dhcpOptions = new List<Tuple<DhcpOptions, string>>(),
                    MaxClients = 1024
                };

                return new Lazy<OpenVPNConfig>(config);
            }
        }

        public static List<Tuple<string, int, TransportProtocols, string>> OpenVPNListeners => new List<Tuple<string, int, TransportProtocols, string>>
        {
            {Tuple.Create("0.0.0.0", 1194, TransportProtocols.TCP, "172.16.224.0/24")},
            {Tuple.Create("0.0.0.0", 1194, TransportProtocols.UDP, "172.16.225.0/24")}
        };
    }
}