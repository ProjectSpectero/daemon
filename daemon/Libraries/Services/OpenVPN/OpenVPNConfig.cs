using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RazorLight;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPNConfig : IServiceConfig
    {
        [JsonIgnore]
        private readonly IRazorLightEngine _engine;
        
        [JsonIgnore]
        public readonly IIdentityProvider Identity;

        [JsonIgnore]
        public X509Certificate2 CACert;
        
        [JsonIgnore]
        public X509Certificate2 ServerCert;
        
        [JsonIgnore]
        public string PKCS12Certificate;
        
        public OpenVPNListener Listener;
        
        public bool AllowMultipleConnectionsFromSameClient;
        public bool ClientToClient;
        public List<Tuple<DhcpOptions, string>> DhcpOptions;
        public int MaxClients;
        public List<string> PushedNetworks;
        public List<RedirectGatewayOptions> RedirectGateway;


        /// <summary>
        /// Constructor with dependency injection.
        ///
        /// --push option
        /// Push a config file option back to the client for remote execution. Note that option must be enclosed in double quotes (""). The client must specify --pull in its config file.
        ///
        /// The set of options which can be pushed is limited by both feasibility and security. Some options such as those which would execute scripts are banned,
        /// since they would effectively allow a compromised server to execute arbitrary code on the client.
        ///
        /// Other options such as TLS or MTU parameters cannot be pushed because the client needs to know them before the connection to the server can be initiated.
        ///
        /// This is a partial list of options which can currently be pushed: --route, --route-gateway, --route-delay, --redirect-gateway
        /// --ip-win32, --dhcp-option, --inactive, --ping, --ping-exit, --ping-restart, --setenv, --persist-key, --persist-tun, --echo
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="identity"></param>
        public OpenVPNConfig(IRazorLightEngine engine, IIdentityProvider identity)
        {
            _engine = engine;
            Identity = identity;
        }

        /// <summary>
        /// Get the Compiled Configuration Template.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetStringConfig()
        {
            // Check if the engine exists.
            if (_engine == null)
                // The engine does not exist, throw an exception.
                throw RazorLightEngineMissingException();

            // Return the rendered template.
            return await _engine.CompileRenderAsync("OpenVPN", this);
        }

        /// <summary>
        /// Exception: throw this when the engine is missing.
        /// </summary>
        /// <returns></returns>
        public Exception RazorLightEngineMissingException()
        {
            return new Exception("# RazorLight Engine is missing - cannot convert.");
        }
    }
}