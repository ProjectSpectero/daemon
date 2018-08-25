using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Models;

namespace Spectero.daemon.Seeds
{
    public class ConfigurationSeed : BaseSeed
    {
        private readonly IDbConnection _db;
        private readonly ILogger<FirstInitSeed> _logger;
        private readonly AppConfig _config;
        private readonly ICryptoService _cryptoService;

        public ConfigurationSeed(IServiceProvider serviceProvider)
        {
            _db = serviceProvider.GetRequiredService<IDbConnection>();
            _logger = serviceProvider.GetRequiredService<ILogger<FirstInitSeed>>();
            _config = serviceProvider.GetRequiredService<IOptionsMonitor<AppConfig>>().CurrentValue;
            _cryptoService = serviceProvider.GetRequiredService<ICryptoService>();
        }

        public override void Up()
        {
            var instanceId = Guid.NewGuid().ToString();
            var localIPs = Utility.GetLocalIPs(_config.IgnoreRFC1918);

            _logger.LogDebug("Firstrun: Seeding default configuration values.");

            // Identity
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.SystemIdentity,
                Value = instanceId
            });

            // Schema version
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.SchemaVersion,
                Value = AppConfig.version
            });

            // Cloud Connectivity
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.CloudConnectStatus,
                Value = false.ToString()
            });

            // HTTP Proxy Configuration
            var httpSkeleton = Defaults.HTTP.Value;
            var proposedListeners = localIPs.Select(ip => Tuple.Create(ip.ToString(), 10240)).ToList();
            if (proposedListeners.Count > 0) httpSkeleton.listeners = proposedListeners;
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.HttpConfig,
                Value = JsonConvert.SerializeObject(httpSkeleton)
            });

            // Determine the password hashing cost.
            _logger.LogDebug("Firstrun: Calculating optimal password hashing cost.");
            var viablePasswordCost = AuthUtils.GenerateViableCost(_config.PasswordCostCalculationTestTarget,
                _config.PasswordCostCalculationIterations,
                _config.PasswordCostTimeThreshold, _config.PasswordCostLowerThreshold);
            
            // Tell the console we have a value.
            _logger.LogDebug($"Firstrun: Determined {viablePasswordCost} to be the optimal password hashing cost.");
            
            // Insert Password information into database.
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.PasswordHashingCost,
                Value = viablePasswordCost.ToString()
            });

            // JWT security Key
            _logger.LogDebug("Firstrun: Generating JWT security key.");
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.JWTSymmetricSecurityKey,
                Value = PasswordUtils.GeneratePassword(48, 8)
            });

            /*
             * Cryptography
             * Create Server and Certificate Authority Passwords. 
             */
            var caPassword = PasswordUtils.GeneratePassword(48, 8);
            var serverPassword = PasswordUtils.GeneratePassword(48, 8);

            // Generate Certificate Authory
            var ca = _cryptoService.CreateCertificateAuthorityCertificate($"CN={instanceId}.ca.instance.spectero.io", null, null, caPassword);

            // Generate Server Certificate Authority Key.
            var serverCertificate = _cryptoService.IssueCertificate($"CN={instanceId}.instance.spectero.io", ca, null,
                new[] {KeyPurposeID.AnyExtendedKeyUsage, KeyPurposeID.IdKPServerAuth}, serverPassword,
                new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));

            // Generate Public Key
            var specteroCertKey = PasswordUtils.GeneratePassword(48, 8);
            var specteroCertificate = _cryptoService.IssueCertificate("CN=" + "spectero", ca, null, new[] {KeyPurposeID.IdKPClientAuth}, specteroCertKey);

            // Store CA Password
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.CeritificationAuthorityPassword,
                Value = caPassword
            });

            // Store Server Password
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.ServerCertificatePassword,
                Value = serverPassword
            });

            // Store CA
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.CertificationAuthority,
                Value = Convert.ToBase64String(_cryptoService.GetCertificateBytes(ca, caPassword))
            });

            // Store Server Certificate.
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.ServerCertificate,
                Value = Convert.ToBase64String(_cryptoService.GetCertificateBytes(serverCertificate, serverPassword))
            });

            // Store Chain.
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.ServerPFXChain,
                // Yes, passwordless. Somewhat intentionally, as this is mostly consumed by 3rd party apps.
                Value = Convert.ToBase64String(_cryptoService.ExportCertificateChain(serverCertificate, ca))
            });

            // OpenVPN defaults
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.OpenVPNListeners,
                Value = JsonConvert.SerializeObject(Defaults.OpenVPNListeners)
            });

            // Store OpenVPN Base Configuration Template.
            _db.Insert(new Configuration
            {
                Key = ConfigKeys.OpenVPNBaseConfig,
                Value = JsonConvert.SerializeObject(Defaults.OpenVPN.Value)
            });
        }

        public override void Down()
        {
            throw new System.NotImplementedException();
        }

        public override string GetVersion()
        {
            throw new System.NotImplementedException();
        }
    }
}