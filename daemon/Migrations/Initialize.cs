using System.Data;
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

        public Initialize(IOptionsMonitor<AppConfig> config, IDbConnection db)
        {
            _db = db;
            _config = config.CurrentValue;
        }

        public void Up()
        {
            if (!_db.TableExists<User>())
                _db.CreateTable<User>();

            if (!_db.TableExists<Statistic>())
                _db.CreateTable<Statistic>();

            if (!_db.TableExists<Configuration>())
            {
                _db.CreateTable<Configuration>();
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpListener,
                    Value = Defaults.HTTP.ToJson()
                });
                _db.Insert(new Configuration
                {
                    Key = ConfigKeys.HttpMode,
                    Value = HTTPProxyModes.Normal.ToJson()
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