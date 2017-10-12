using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Libraries.Core.Statistics
{
    public class Statistician : IStatistician
    {
        private readonly AppConfig _appConfig;
        private readonly IDbConnection _db;
        private readonly ILogger<Statistician> _logger;

        public Statistician(IOptionsMonitor<AppConfig> appConfig, ILogger<Statistician> logger, IDbConnection db)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
            _db = db;
        }

        public async Task<bool> Update<T>(long bytes, DataFlowDirections direction) where T : new()
        {
            _logger.LogDebug(string.Format("BDU: Logging {0} bytes in the {1} direction as requested by {2}", bytes,
                direction.ToString(), typeof(T)));
            return false;
        }
    }
}