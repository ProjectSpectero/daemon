using System;
using System.Collections.Generic;
using System.Net;
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
                    Tuple.Create(IPAddress.Any.ToString(), 10240)
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
                    PushedNetworks = new List<string>(),
                    RedirectGateway = new List<RedirectGatewayOptions> {RedirectGatewayOptions.Def1},
                    DhcpOptions = new List<Tuple<DhcpOptions, string>>(),
                    MaxClients = 1024
                };

                return new Lazy<OpenVPNConfig>(config);
            }
        }

        public static List<OpenVPNListener> OpenVPNListeners => new List<OpenVPNListener>
        {
            new OpenVPNListener
            {
                IPAddress = "0.0.0.0",
                Protocol = TransportProtocol.TCP,
                Port = 1194,
                ManagementPort = 35100, // TODO: Define a formal "Port Management" service to allocate (and additionally forward through NAT) these.
                Network = "172.16.224.0/24"
            },
            new OpenVPNListener
            {
                IPAddress = "0.0.0.0",
                Protocol = TransportProtocol.UDP,
                Port = 1194,
                ManagementPort = 35101, // TODO: Define a formal "Port Management" service to allocate (and additionally forward through NAT) these.
                Network = "172.16.225.0/24"
            }
        };
    }
}