using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Models;

namespace Spectero.daemon.Seeds
{
    public class UserSeed : BaseSeed
    {
        private readonly IDbConnection _db;
        private readonly ILogger<FirstInitSeed> _logger;
        private readonly AppConfig _config;
        private readonly ICryptoService _cryptoService;

        public UserSeed(IServiceProvider serviceProvider)
        {
            _db = serviceProvider.GetRequiredService<IDbConnection>();
            _logger = serviceProvider.GetRequiredService<ILogger<FirstInitSeed>>();
            _config = serviceProvider.GetRequiredService<IOptionsMonitor<AppConfig>>().CurrentValue;
            _cryptoService = serviceProvider.GetRequiredService<ICryptoService>();
        }

        public override void Up()
        {
            // Tell the cosnole.
            _logger.LogDebug("Firstrun: Seeding Users table");

            // Get password requirements from database
            var passwordHashingCost = _db.Select<Configuration>(x => x.Key.Contains(ConfigKeys.PasswordHashingCost)).First().Value;

            // Generate a user password.
            var userPassword = PasswordUtils.GeneratePassword(16, 8);

            // TODO: Talk to paul about safely managing the CertKey.
            // Get the certificate key
            var certificateKey = "";
            
            // Get the certificate information from the configuration table.
            X509Certificate2 certificateAuthority = ImportX509Certificate2(
                _db.Select<Configuration>(x => x.Key.Contains(ConfigKeys.CertificationAuthority)).First().Value,
                _db.Select<Configuration>(x => x.Key.Contains(ConfigKeys.ServerCertificatePassword)).First().Value
            );   
            X509Certificate2 certificate = _cryptoService.IssueCertificate("CN=spectero", certificateAuthority, null, new[] {KeyPurposeID.IdKPClientAuth}, certificateKey);

            // Determine Cert value
            var cert = "";
            if (certificate != null && certificateAuthority != null)
                cert = Convert.ToBase64String(_cryptoService.ExportCertificateChain(certificate, certificateAuthority, certificateKey));

            // Insert user into table.
            _db.Insert(new User
            {
                AuthKey = "spectero",
                Roles = new List<User.Role>
                {
                    User.Role.SuperAdmin
                },
                FullName = "Spectero Administrator",
                EmailAddress = "changeme@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword(userPassword, int.Parse(passwordHashingCost)),
                Cert = cert,
                CertKey = certificateKey,
                Source = User.SourceTypes.Local,
                CreatedDate = DateTime.Now
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

        private X509Certificate2 ImportX509Certificate2(string base64EncodedCertificateString, string password = "password")
        {
            var decodedBase64 = Convert.FromBase64String(base64EncodedCertificateString);
            return _cryptoService.LoadCertificate(decodedBase64, password);
        }
    }
}