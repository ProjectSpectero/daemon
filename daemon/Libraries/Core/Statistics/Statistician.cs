﻿using System;
using System.Data;
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

        public bool Update<T> (double bytes, DataFlowDirections direction) where T : new()
        {
            _logger.LogDebug(String.Format("BDU: Logging {0} bytes in the {1} direction as requested by {2}", bytes, direction.ToString(), typeof(T)));
            return false;
        }
    }
}