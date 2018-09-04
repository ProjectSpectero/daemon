using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;

namespace Spectero.daemon.Seeds
{
    public class UpgradeSchemaVersionTo02 : BaseSeed
    {
        public UpgradeSchemaVersionTo02(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<UpgradeSchemaVersionTo02>>();
            
        }
        
        public override void Up()
        {
            var existingVersion = _db.Select<Configuration>(x => x.Key == ConfigKeys.SchemaVersion)
                .FirstOrDefault();
            
            if (existingVersion != null)
            {
                // OK, a version has previously been assigned.
                if (existingVersion.Value.Equals(AppConfig.Version)) return;
                
                _logger.LogInformation($"Upgrading schema version from {existingVersion.Value} to {AppConfig.Version}");
                
                ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.SchemaVersion, AppConfig.Version)
                    .Wait();
            }
            else
            {
                _logger.LogInformation($"Initializing schema version to {AppConfig.Version}");
                
                // OK, this is the first time. We simply blindly insert.
                ConfigUtils.CreateOrUpdateConfig(_db, ConfigKeys.SchemaVersion, AppConfig.Version)
                    .Wait();
            }
        }

        public override void Down()
        {
            throw new System.NotImplementedException();
        }

        public override string GetVersion()
        {
            return "20180904131526";
        }
    }
}