using System;
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
        private readonly ILogger<Statistician> _logger;
        private readonly IDbConnection _db;
        
        public Statistician (IOptionsMonitor<AppConfig> appConfig, ILogger<Statistician> logger, IDbConnection db)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
            _db = db;
        }

        public async Task<bool> Update<T> (double bytes, DataFlowDirections direction) where T : new()
        {
            Console.WriteLine(string.Format("BDU: Logging {0} bytes in the {1} direction as requested by {2}", bytes, direction.ToString(), typeof(T)));
            //_logger.LogDebug();
            return false;
        }
    }
}