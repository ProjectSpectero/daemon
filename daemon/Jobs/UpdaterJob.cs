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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            if (releaseInformation.channels.GetValueOrDefault(_config.Updater.ReleaseChannel.ToString(), AppConfig.version) != AppConfig.version)
            {
                // Update available.
                var newVersion = releaseInformation.channels[_config.Updater.ReleaseChannel.ToString()];
                var targetDirectory = Path.Combine(RootInstallationDirectory, newVersion);
                var targetArchive = Path.Combine(RootInstallationDirectory, "${newVersion}.zip");

                // Log to the console that the update is available.
                _logger.LogInformation("There is a update available for the spectero daemon: " + newVersion);

                // Get the download link
                var downloadLink = releaseInformation.versions[newVersion].download;

                // Download
                using (WebClient webClient = new WebClient())
                    webClient.DownloadFile(new Uri(downloadLink), targetArchive);

                // Extract
                ZipFile.ExtractToDirectory(targetArchive, targetDirectory);

                // Copy the databases
                foreach (string databasePath in GetDatabasePaths())
                {
                    var basename = new FileInfo(databasePath).Name;
                    var databaseDestPath = Path.Combine(targetDirectory, "daemon", "Database", basename);
                    File.Copy(databasePath, databaseDestPath);
                }

                // Fix the symlinks.
                // TODO: Invoke System - May need to be platform specific.
               

                // Restart the service.
                // TODO: Overview the instllation scripts and make sure they use the latest directory
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

        private string RootInstallationDirectory => Directory.GetParent(
            Directory.GetParent(Program.GetAssemblyLocation()).FullName
        ).FullName;

        private string DatabaseDirectory => Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir);

        private string[] GetDatabasePaths() => Directory.GetFiles(DatabaseDirectory, "*.sqlite", SearchOption.TopDirectoryOnly);
    }
}