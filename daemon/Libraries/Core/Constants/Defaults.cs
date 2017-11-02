using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;

namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Defaults
    {
        public static List<Tuple<string, int>> HTTP
        {
            get
            {
                var ret = new List<Tuple<string, int>>();
                ret.Add(Tuple.Create(IPAddress.Any.ToString(), 8800));
                return ret;
            }
        }

        public static OpenVPNConfig OpenVPN
        {
            get
            {
                var config = new OpenVPNConfig(null, null);
                config.AllowMultipleConnectionsFromSameClient = false;
                config.ClientToClient = false;

                config.pushedNetworks = new List<IPNetwork>();

                config.redirectGateway = new List<RedirectGatewayOptions>();
                config.redirectGateway.Add(RedirectGatewayOptions.Def1);

                config.dhcpOptions = new List<Tuple<DhcpOptions, string>>();

                config.PKCS12Certificate = "";

                return config;
            }
        }


    }
}