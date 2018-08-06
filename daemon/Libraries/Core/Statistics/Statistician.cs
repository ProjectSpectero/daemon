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