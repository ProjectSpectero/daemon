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
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RazorLight;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Config
{
    /// <summary>
    /// CertificateInformation
    /// I've refactored this into a more simplistic object for easy micromanagement, 
    /// and renamed them to seem a bit more organized.
    /// 
    /// Please make any changes nessasary. 
    /// </summary>
    public struct CertificateBaseInformation
    {
        public string CertificateAuthorityBase64PKCS12;
        public string CertificateAuthoryPassword;
        public string ServerBase64PKCS12;
        public string ServerKeyChainBase64PKCS12;
        public string ServerCertificatePassword;
    }

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
                            _logger.LogWarning("TG: No listeners could be retrieved from the DB for " +
                                             typeof(HTTPProxy) + ", using defaults. Please consider re-installing or restoring the database from backups.");
                            serviceConfig = Defaults.HTTP.Value;
                        }

                        return new List<IServiceConfig> {serviceConfig};
                    }
                },
                {
                    typeof(OpenVPN), delegate
                    {
                        // In scope OpenVPN Configuration List.
                        var configs = new List<OpenVPNConfig>();

                        // Select attributes from local database.
                        var storedOpenVPNConfig = _db.Select<Configuration>(x => x.Key.Contains("vpn.openvpn."));
                        var storedCryptoConfig = _db.Select<Configuration>(x => x.Key.Contains("crypto."));

                        // Check if values exist.
                        if (storedOpenVPNConfig == null || storedCryptoConfig == null)
                            throw new InternalError("TG: Could not fetch OpenVPN or Crypto config from the database. Please re-install or restore the database from backups, no defaults are possible for the CA/PKI parameters.");

                        // Certificate Information Placeholder Object.
                        var certInfo = new CertificateBaseInformation();

                        // Iterate through each configuration key.
                        foreach (var cryptoConfig in storedCryptoConfig)
                        {
                            switch (cryptoConfig.Key)
                            {
                                case ConfigKeys.ServerPFXChain:
                                    certInfo.ServerKeyChainBase64PKCS12 = cryptoConfig.Value;
                                    break;

                                case ConfigKeys.ServerCertificate:
                                    certInfo.ServerBase64PKCS12 = cryptoConfig.Value;
                                    break;

                                case ConfigKeys.ServerCertificatePassword:
                                    certInfo.ServerCertificatePassword = cryptoConfig.Value;
                                    break;

                                case ConfigKeys.CertificationAuthority:
                                    certInfo.CertificateAuthorityBase64PKCS12 = cryptoConfig.Value;
                                    break;

                                case ConfigKeys.CeritificationAuthorityPassword:
                                    certInfo.CertificateAuthoryPassword = cryptoConfig.Value;
                                    break;
                            }
                        }

                        // Check to see if any value is null.
                        if (
                            certInfo.ServerKeyChainBase64PKCS12.IsNullOrEmpty() ||
                            certInfo.CertificateAuthorityBase64PKCS12.IsNullOrEmpty() ||
                            certInfo.ServerBase64PKCS12.IsNullOrEmpty() ||
                            certInfo.CertificateAuthoryPassword.IsNullOrEmpty() ||
                            certInfo.ServerCertificatePassword.IsNullOrEmpty()
                        )
                        {
                            throw new InternalError("TG: One or more crypto parameters are invalid, please re-install or restore the database from backups.");
                        }

                        var certificateAuthory = _cryptoService.LoadCertificate(
                            Convert.FromBase64String(certInfo.CertificateAuthorityBase64PKCS12),
                            certInfo.CertificateAuthoryPassword
                        );
                        var serverCertificate = _cryptoService.LoadCertificate(
                            Convert.FromBase64String(certInfo.ServerBase64PKCS12),
                            certInfo.ServerCertificatePassword
                        );

                        var baseOpenVPNConfigInJson =
                            _db.Single<Configuration>(x => x.Key == ConfigKeys.OpenVPNBaseConfig);
                        var baseOpenVPNConfig = JsonConvert.DeserializeObject<OpenVPNConfig>(baseOpenVPNConfigInJson.Value);

                        var listenerConfigInJson = _db.Single<Configuration>(x => x.Key == ConfigKeys.OpenVPNListeners);
                        var listeners = JsonConvert.DeserializeObject<List<OpenVPNListener>>(listenerConfigInJson.Value);

                        // Make sure there's configuration information in the database.
                        if (baseOpenVPNConfig == null || listeners == null || listeners.Count == 0)
                        {
                            _logger.LogWarning("TG: Could not fetch OpenVPN config from the database, using defaults. Please consider re-installing or restoring the database from backups.");
                            
                            baseOpenVPNConfig = Defaults.OpenVPN.Value;
                            listeners = Defaults.OpenVPNListeners;
                        }

                        // Write configurations for each.
                        foreach (var listener in listeners)
                        {
                            var localConfig = new OpenVPNConfig(_engine, _identity) {Listener = listener};
                            configs.Add(localConfig);
                        }

                        // Write objects for each configuration
                        foreach (var cfg in configs)
                        {
                            cfg.CACert = certificateAuthory;
                            cfg.ServerCert = serverCertificate;
                            cfg.PKCS12Certificate = certInfo.ServerKeyChainBase64PKCS12;
                            cfg.AllowMultipleConnectionsFromSameClient = baseOpenVPNConfig.AllowMultipleConnectionsFromSameClient;
                            cfg.ClientToClient = baseOpenVPNConfig.ClientToClient;
                            cfg.MaxClients = baseOpenVPNConfig.MaxClients;
                            cfg.DhcpOptions = baseOpenVPNConfig.DhcpOptions;
                            cfg.RedirectGateway = baseOpenVPNConfig.RedirectGateway;
                            cfg.PushedNetworks = baseOpenVPNConfig.PushedNetworks;
                        }

                        // Return the list of configurations.
                        return configs;
                    }
                }
            };

            // Try to return the value.
            if (processors.TryGetValue(type, out var value))
                return value();

            // Failed, return null.
            return null;
        }
    }
}