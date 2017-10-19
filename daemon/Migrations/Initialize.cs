using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;

namespace Spectero.daemon.Migrations
{
    public class Initialize : IMigration
    {
        private readonly AppConfig _config;
        private readonly IDbConnection _db;
        private readonly ILogger<Initialize> _logger;


        public Initialize(IOptionsMonitor<AppConfig> config, ILogger<Initialize> logger, IDbConnection db)
        {
            _config = config.CurrentValue;
            _logger = logger;
            _db = db;
        }

        public void Up()
        {
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
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.PasswordHashingCost,
                    Value = AuthUtils.GenerateViableCost(_config.PasswordCostCalculationTestTarget,
                            _config.PasswordCostCalculationIterations,
                            _config.PasswordCostTimeThreshold, _config.PasswordCostLowerThreshold)
                        .ToString()
                });
            }
        }

        public void Down()
        {
        }
    }
}