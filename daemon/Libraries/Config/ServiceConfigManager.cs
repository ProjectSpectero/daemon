using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RazorLight;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Config
{
    public class ServiceConfigManager : IServiceConfigManager
    {
        private readonly AppConfig _appConfig;
        private readonly IDbConnection _db;
        private readonly ILogger<ServiceConfigManager> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IRazorLightEngine _engine;
        private readonly IIdentityProvider _identity;

        public ServiceConfigManager(IOptionsMonitor<AppConfig> config, ILogger<ServiceConfigManager> logger,
            IDbConnection db, ICryptoService cryptoService,
            IRazorLightEngine razorLightEngine, IIdentityProvider identityProvider)
        {
            _appConfig = config.CurrentValue;
            _logger = logger;
            _db = db;
            _cryptoService = cryptoService;
            _engine = razorLightEngine;
            _identity = identityProvider;
        }

        public IServiceConfig Generate<T>() where T : new()
        {
            var type = typeof(T);
            var processors = new Dictionary<Type, Func<IServiceConfig>>
            {
                {
                    typeof(HTTPProxy), delegate
                    {
                        var listeners = new List<Tuple<string, int>>();

                        var serviceConfig = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpListener);

                        if (serviceConfig.Count > 0)
                        {
                            foreach (var listener in serviceConfig)
                            {
                                var lstDict = JsonConvert
                                    .DeserializeObject<List<Dictionary<string, dynamic>>>(listener.Value)
                                    .FirstOrDefault();
                                if (lstDict != null && lstDict.ContainsKey("Item1") && lstDict.ContainsKey("Item2"))
                                {
                                    var ip = (string) lstDict["Item1"];
                                    var port = (int) lstDict["Item2"];
                                    listeners.Add(Tuple.Create(ip, port));
                                }
                                else
                                {
                                    _logger.LogError(
                                        "TG: Could not extract a valid ip:port pair from at least one listener.");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("TG: No listeners could be retrieved from the DB for " +
                                               typeof(HTTPProxy) + ", using defaults.");
                            listeners = Defaults.HTTP;
                        }

                        var proxyMode = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpMode)
                            .FirstOrDefault(); // Guaranteed to be one single value
                        var allowedDomains = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpAllowedDomains)
                            .FirstOrDefault(); // JSON list of strings, but there is only one list
                        var bannedDomains = _db.Select<Configuration>(x => x.Key == ConfigKeys.HttpBannedDomains)
                            .FirstOrDefault(); // JSON list of strings, but there is only one list

                        var actualMode = HTTPProxyModes.Normal;
                        var actualAllowedDomains = new List<string>();
                        var actualBannedDomains = new List<string>();

                        if (proxyMode != null)
                            if (proxyMode.Value == HTTPProxyModes.ExclusiveAllow.ToString())
                                actualMode = HTTPProxyModes.ExclusiveAllow;

                        if (allowedDomains != null)
                            actualAllowedDomains = JsonConvert
                                .DeserializeObject<List<string>>(allowedDomains.Value);

                        if (bannedDomains != null)
                            actualBannedDomains = JsonConvert
                                .DeserializeObject<List<string>>(bannedDomains.Value);

                        return new HTTPConfig(listeners, actualMode, actualAllowedDomains, actualBannedDomains);
                    }
                },
                {
                    typeof(OpenVPN), delegate
                    {
                        List<OpenVPNConfig> configs = new List<OpenVPNConfig>();

                        var storedOpenVPNConfig = _db.Select<Configuration>(x => x.Key.Contains("vpn.openvpn."));
                        var storedCryptoConfig = _db.Select<Configuration>(x => x.Key.Contains("crypto."));

                        if (storedOpenVPNConfig == null || storedCryptoConfig == null)
                        {
                            _logger.LogError("TG: Could not fetch OpenVPN or Crypto config from the database. Please re-install, no defaults are possible for the CA/PKI.");
                            return null;
                        }
                            

                        var config = new OpenVPNConfig(_engine, _identity);

                        var base64CAPKCS12 = "";
                        var base64ServerPKCS12 = "";
                        var base64ServerChainPKCS12 = "";
                        var caPass = "";
                        var serverCertPass = "";

                        foreach (var cryptoConfig in storedCryptoConfig)
                        {
                            switch (cryptoConfig.Key)
                            {
                                    case ConfigKeys.ServerPFXChain:
                                        base64ServerChainPKCS12 = cryptoConfig.Value;
                                    break;
                                    case ConfigKeys.ServerCertificate:
                                        base64ServerPKCS12 = cryptoConfig.Value;
                                    break;
                                    case ConfigKeys.ServerCertificatePassword:
                                        serverCertPass = cryptoConfig.Value;
                                    break;
                                    case ConfigKeys.CertificationAuthority:
                                        base64CAPKCS12 = cryptoConfig.Value;
                                    break;
                                    case ConfigKeys.CeritificationAuthorityPassword:
                                        caPass = cryptoConfig.Value;
                                    break;
                            }
                        }

                        if (base64ServerChainPKCS12.IsNullOrEmpty() || base64CAPKCS12.IsNullOrEmpty() ||
                            base64ServerPKCS12.IsNullOrEmpty() || caPass.IsNullOrEmpty() ||
                            serverCertPass.IsNullOrEmpty())
                        {
                            _logger.LogError("TG: One or more crypto parameters are invalid, please re-install.");
                            return null;
                        }

                        var ca = _cryptoService.LoadCertificate(Convert.FromBase64String(base64CAPKCS12), caPass);
                        var serverCert = _cryptoService.LoadCertificate(Convert.FromBase64String(base64ServerPKCS12),
                            serverCertPass);

                        bool AllowClientToClient = Defaults.OpenVPN.ClientToClient;
                        bool AllowMultipleConnectionsFromSameClient = Defaults.OpenVPN.AllowMultipleConnectionsFromSameClient;
                        int MaxClients = Defaults.OpenVPN.MaxClients;

                        var dhcpOptions = Defaults.OpenVPN.dhcpOptions;
                        var listeners = new List<Tuple<string, int, TransportProtocols, IPNetwork>>();
                        var pushedNetworks = Defaults.OpenVPN.pushedNetworks;
                        var redirectGatewayOptions = Defaults.OpenVPN.redirectGateway;

                        foreach (var ovpnConfig in storedOpenVPNConfig)
                        {
                            switch (ovpnConfig.Key)
                            {
                                    case ConfigKeys.OpenVPNAllowClientToClient:
                                        AllowClientToClient = Boolean.Parse(ovpnConfig.Value);
                                    break;
                                    case ConfigKeys.OpenVPNAllowMultipleConnectionsFromSameClient:
                                        AllowMultipleConnectionsFromSameClient = Boolean.Parse(ovpnConfig.Value);
                                    break;
                                    case ConfigKeys.OpenVPNDHCPOptions:
                                        //TODO: Validate that this conversion is proper [1]
                                        var options = JsonConvert.DeserializeObject<List<Tuple<DhcpOptions, string>>>(ovpnConfig.Value);
                                        if (options != null)
                                            dhcpOptions = options;
                                        break;
                                    case ConfigKeys.OpenVPNListeners:
                                        //TODO: Validate that this conversion is proper [2]
                                        var networkListeners = JsonConvert.DeserializeObject<List<Tuple<string, int, TransportProtocols, IPNetwork>>>(ovpnConfig.Value);
                                        if (networkListeners != null)
                                            listeners = networkListeners;
                                    break;
                                    case ConfigKeys.OpenVPNMaxClients:
                                        int.TryParse(ovpnConfig.Value, out MaxClients);
                                    break;
                                    case ConfigKeys.OpenVPNPushedNetworks:
                                        //TODO: Validate that this conversion is proper [3]
                                        var networks = JsonConvert.DeserializeObject<List<IPNetwork>>(ovpnConfig.Value);
                                        if (networks != null)
                                            pushedNetworks = networks;

                                        //IPNetwork.TryParse(ovpnConfig.Value, out var tmpNetwork);
                                        //if (tmpNetwork != null)
                                        //    pushedNetworks.Add(tmpNetwork);
                                    break;
                                    case ConfigKeys.OpenVPNRedirectGatewayOptions:
                                        //TODO: Validate that this conversion is proper [4]
                                        var redirectOptions = JsonConvert.DeserializeObject<List<RedirectGatewayOptions>>(ovpnConfig.Value);
                                        if (redirectOptions != null)
                                            redirectGatewayOptions = redirectOptions;
                                    break;
                            }
                        }

                        foreach (var listener in listeners)
                        {
                            var localConfig = new OpenVPNConfig(_engine, _identity);
                            localConfig.listener = listener;
                            configs.Add(localConfig);
                        }

                        foreach (var cfg in configs)
                        {
                            cfg.CACert = ca;
                            cfg.ServerCert = serverCert;
                            cfg.PKCS12Certificate = base64ServerChainPKCS12;
                            cfg.AllowMultipleConnectionsFromSameClient = AllowMultipleConnectionsFromSameClient;
                            cfg.ClientToClient = AllowClientToClient;
                            cfg.MaxClients = MaxClients;
                            cfg.dhcpOptions = dhcpOptions;
                            cfg.redirectGateway = redirectGatewayOptions;
                            cfg.pushedNetworks = pushedNetworks;
                        }

                        // TODO: Expand API to allow exporting an IEnurable<IServiceConfig> instead
                        return configs.First();
                    }
                }
            };

            return processors[type]();
        }
    }
}