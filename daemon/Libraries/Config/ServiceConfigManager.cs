using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        public IEnumerable<IServiceConfig> Generate(Type type)
        {
            var processors = new Dictionary<Type, Func<IEnumerable<IServiceConfig>>>
            {
                {
                    typeof(HTTPProxy), delegate
                    {
                        var storedConfig = _db.Single<Configuration>(x => x.Key == ConfigKeys.HttpConfig);
                        var serviceConfig = JsonConvert.DeserializeObject<HTTPConfig>(storedConfig.Value);


                        if (serviceConfig == null)
                        {
                            _logger.LogError("TG: No listeners could be retrieved from the DB for " +
                                               typeof(HTTPProxy) + ", using defaults.");
                            serviceConfig = Defaults.HTTP.Value;
                        }

                        return new List<IServiceConfig>{ serviceConfig };
                    }
                },
                {
                    typeof(OpenVPN), delegate
                    {
                        var configs = new List<OpenVPNConfig>();

                        var storedOpenVPNConfig = _db.Select<Configuration>(x => x.Key.Contains("vpn.openvpn."));
                        var storedCryptoConfig = _db.Select<Configuration>(x => x.Key.Contains("crypto."));

                        if (storedOpenVPNConfig == null || storedCryptoConfig == null)
                        {
                            _logger.LogError("TG: Could not fetch OpenVPN or Crypto config from the database. Please re-install, no defaults are possible for the CA/PKI.");
                            return null;
                        }
                            
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

                        var baseOpenVPNConfigInJson =
                            _db.Single<Configuration>(x => x.Key == ConfigKeys.OpenVPNBaseConfig);
                        var baseOpenVPNConfig = JsonConvert.DeserializeObject<OpenVPNConfig>(baseOpenVPNConfigInJson.Value);

                        var listenerConfigInJson = _db.Single<Configuration>(x => x.Key == ConfigKeys.OpenVPNListeners);
                        var listeners =
                            JsonConvert.DeserializeObject<List<OpenVPNListener>>(
                                listenerConfigInJson.Value);

                        if (baseOpenVPNConfig == null || listeners == null || listeners.Count == 0)
                        {
                            _logger.LogError("TG: Could not fetch OpenVPN config from the database. Using defaults.");
                            baseOpenVPNConfig = Defaults.OpenVPN.Value;
                            listeners = Defaults.OpenVPNListeners;
                        }

                       
                        foreach (var listener in listeners)
                        {
                            var localConfig = new OpenVPNConfig(_engine, _identity) {Listener = listener};
                            configs.Add(localConfig);
                        }

                        foreach (var cfg in configs)
                        {
                            cfg.CACert = ca;
                            cfg.ServerCert = serverCert;
                            cfg.PKCS12Certificate = base64ServerChainPKCS12;
                            cfg.AllowMultipleConnectionsFromSameClient = baseOpenVPNConfig.AllowMultipleConnectionsFromSameClient;
                            cfg.ClientToClient = baseOpenVPNConfig.ClientToClient;
                            cfg.MaxClients = baseOpenVPNConfig.MaxClients;
                            cfg.DhcpOptions = baseOpenVPNConfig.DhcpOptions;
                            cfg.RedirectGateway = baseOpenVPNConfig.RedirectGateway;
                            cfg.PushedNetworks = baseOpenVPNConfig.PushedNetworks;
                        }

                        return configs;
                    }
                }
            };

            if (processors.TryGetValue(type, out var value))
                return value();

            return null;
        }
    }
}