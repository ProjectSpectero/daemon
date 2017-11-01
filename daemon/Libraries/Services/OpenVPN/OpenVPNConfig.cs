using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto.Parameters;
using RazorLight;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPNConfig : IServiceConfig
    {
        private readonly IRazorLightEngine _engine;
        private readonly string serviceName = "OpenVPN";
        public IIdentityProvider _identity;
        public bool AllowMultipleConnectionsFromSameClient;
        public X509Certificate2 CACert;
        /*
         * string = IP address
         * int = port
         * Transportproto = tcp/udp server
         * IPNetwork = Local subnet
         */
        public Tuple<string, int, TransportProtocols, IPNetwork> listener;
        public bool ClientToClient;
        public List<Tuple<DhcpOptions, string>> dhcpOptions;
        public int MaxClients;
        public List<IPNetwork> pushedNetworks;
        public List<RedirectGatewayOptions> redirectGateway;
        public X509Certificate2 ServerCert;
        public string PKCS12Certificate;

        /*
         * --push option
         * Push a config file option back to the client for remote execution. Note that option must be enclosed in double quotes (""). The client must specify --pull in its config file.
         * The set of options which can be pushed is limited by both feasibility and security. Some options such as those which would execute scripts are banned,
         * since they would effectively allow a compromised server to execute arbitrary code on the client. 
         * Other options such as TLS or MTU parameters cannot be pushed because the client needs to know them before the connection to the server can be initiated.
         * This is a partial list of options which can currently be pushed: --route, --route-gateway, --route-delay, --redirect-gateway
         * --ip-win32, --dhcp-option, --inactive, --ping, --ping-exit, --ping-restart, --setenv, --persist-key, --persist-tun, --echo
         * 
         */

        public OpenVPNConfig(IRazorLightEngine engine, IIdentityProvider identity)
        {
            _engine = engine;
            _identity = identity;
        }

        public override string ToString()
        {
            if (_engine == null) return "# RazorEngine is missing, can't convert.";
            return _engine.Parse(serviceName, this);
        }
    }
}