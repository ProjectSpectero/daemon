using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Jobs
{
    public class UpdaterConfiguration
    {
        public int ReleaseChannel { get; set; }
        public bool Enabled { get; set; }
        public string Frequency { get; set; }
    }

    public class UpdaterJob : IJob
    {
        // Class Dependencies
        private readonly ILogger<UpdaterJob> _logger;
        private readonly AppConfig _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>
        /// <param name="logger"></param>
        public UpdaterJob(IOptionsMonitor<AppConfig> configMonitor, ILogger<UpdaterJob> logger)
        {
            _logger = logger;
            _config = configMonitor.CurrentValue;

            logger.LogDebug("UJ: init successful, dependencies processed.");
        }

        public string GetSchedule() => _config.Updater.Frequency;

        public bool IsEnabled() => _config.Updater.Enabled;

        public void Perform()
        {
            throw new NotImplementedException();
        }
    }
}