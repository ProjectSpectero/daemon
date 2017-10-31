using System;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;

namespace Spectero.daemon.Migrations
{
    public class Initialize : IMigration
    {
        private readonly AppConfig _config;
        private readonly ICryptoService _cryptoService;
        private readonly IDbConnection _db;
        private readonly IIdentityProvider _identityProvider;
        private readonly ILogger<Initialize> _logger;


        public Initialize(IOptionsMonitor<AppConfig> config, ILogger<Initialize> logger,
            IDbConnection db, IIdentityProvider identityProvider,
            ICryptoService cryptoService)
        {
            _config = config.CurrentValue;
            _logger = logger;
            _db = db;
            _identityProvider = identityProvider;
            _cryptoService = cryptoService;
        }

        public void Up()
        {
            var instanceId = _identityProvider.GetGuid();

            if (!_db.TableExists<User>())
            {
                _logger.LogDebug("Firstrun: Creating Users table");
                _db.CreateTable<User>();
            }


            if (!_db.TableExists<Statistic>())
            {
                _logger.LogDebug("Firstrun: Creating Statistics table");
                _db.CreateTable<Statistic>();
            }


            if (!_db.TableExists<Configuration>())
            {
                _db.CreateTable<Configuration>();
                _logger.LogDebug("Firstrun: Creating Configurations table and inserting default values");

                // Identity
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.SystemIdentity,
                    Value = _identityProvider.GetGuid().ToString()
                });

                // HTTP proxy
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpListener,
                    Value = Defaults.HTTP.ToJson()
                });
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpMode,
                    Value = HTTPProxyModes.Normal.ToString()
                });
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpAllowedDomains,
                    Value = ""
                });
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpBannedDomains,
                    Value = ""
                });
                // Password Hashing
                _logger.LogDebug("Firstrun: Calculating optimal password hashing cost.");
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.PasswordHashingCost,
                    Value = AuthUtils.GenerateViableCost(_config.PasswordCostCalculationTestTarget,
                            _config.PasswordCostCalculationIterations,
                            _config.PasswordCostTimeThreshold, _config.PasswordCostLowerThreshold)
                        .ToString()
                });

                // Crypto
                // 48 characters len with 12 non-alpha-num characters
                // Ought to be good enough for everyone. -- The IPv4 working group, 1996
                var caPassword = PasswordUtils.GeneratePassword(48, 12);
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.CeritificationAuthorityPassword,
                    Value = caPassword
                });
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.CertificationAuthority,
                    Value = Convert.ToBase64String(_cryptoService.CreateCertificateAuthority
                    (
                            "CN=" + instanceId + ".ca.instance.spectero.io", null, null, caPassword
                    ))
                });
            }
        }

        public void Down()
        {
        }
    }
}