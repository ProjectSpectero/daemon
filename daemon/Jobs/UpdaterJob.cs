using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hangfire.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;

namespace Spectero.daemon.Jobs
{
    public class UpdaterConfiguration
    {
        public int ReleaseChannel { get; set; }
        public bool Enabled { get; set; }
        public string Frequency { get; set; }
    }

    public class Release
    {
        public Dictionary<string, string> channels;
        public Dictionary<string, SpecificVersion> versions;
    }

    public class SpecificVersion
    {
        public string download;
        public string altDownload;
        public string changelog;
        public string requiredDotnetCoreVersion;
        public int requiredInstallReversion;
    }

    public class UpdaterJob : IJob
    {
        // Class Dependencies
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdaterJob> _logger;
        private readonly AppConfig _config;
        private JObject _releaseInformation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>`
        /// <param name="logger"></param>
        public UpdaterJob(IOptionsMonitor<AppConfig> configMonitor, ILogger<UpdaterJob> logger, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = configMonitor.CurrentValue;

            logger.LogDebug("UJ: init successful, dependencies processed.");
        }

        public string GetSchedule() => _config.Updater.Frequency;

        public bool IsEnabled() => _config.Updater.Enabled;

        public void Perform()
        {
            // Get the latest set of release data.
            var releaseInformation = GetReleaseInformation();

            // Compare
            if (releaseInformation.channels[_config.Updater.ReleaseChannel.ToString()] != AppConfig.version)
            {
                // Update available.
            }
        }

        private Release GetReleaseInformation()
        {
            try
            {
                var response = _httpClient.GetAsync("https://c.spectero.com/releases.json").Result;
                var releaseData = JsonConvert.DeserializeObject<Release>(response.Content.ReadAsStringAsync().Result);
                return releaseData;
            }
            catch (Exception exception)
            {
                var msg = "UJ: Failed to get release data from the internet.\n" + exception;
                _logger.LogError(msg);
                throw new InternalError(msg);
            }
        }
    }
}