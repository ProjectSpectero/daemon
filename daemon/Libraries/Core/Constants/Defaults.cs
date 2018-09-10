/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
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
        public static readonly int DefinableOpenVPNListenerCount = 32;

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