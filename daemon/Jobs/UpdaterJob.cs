﻿/*
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
using Spectero.daemon.Libraries.Symlink;

namespace Spectero.daemon.Jobs
{
    
    /// <summary>
    /// Configuration blueprint that the appconfig will provide.
    /// </summary>
    public class UpdaterConfiguration
    {
        public int ReleaseChannel { get; set; }
        public bool Enabled { get; set; }
        public string Frequency { get; set; }
    }

    /// <summary>
    /// Release Object to hold JSON of release data.
    /// </summary>
    public class Release
    {
        public Dictionary<string, string> channels;
        public Dictionary<string, SpecificVersion> versions;
    }

    /// <summary>
    /// Specific Version Internal Class for POCO.
    /// </summary>
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
        private readonly Symlink _symlink;
        private JObject _releaseInformation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>`
        /// <param name="logger"></param>
        public UpdaterJob(IOptionsMonitor<AppConfig> configMonitor, ILogger<UpdaterJob> logger, HttpClient httpClient, Symlink symlink)
        {
            _httpClient = httpClient;
            _symlink = symlink;
            _logger = logger;
            _config = configMonitor.CurrentValue;

            logger.LogDebug("UJ: init successful, dependencies processed.");
        }

        /// <summary>
        /// Get the crontab frequency of the job.
        /// </summary>
        /// <returns></returns>
        public string GetSchedule() => _config.Updater.Frequency;

        /// <summary>
        /// Determine if the job is enablked.
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled() => _config.Updater.Enabled;

        /// <summary>
        /// Routine of the job.
        /// </summary>
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
                
                // Get the expected symlink path.
                var latestPath = Path.Combine(RootInstallationDirectory, "latest");
                
                // Delete the symlink if it exists.
                if (_symlink.IsSymlink(latestPath)) _symlink.Environment.Delete(latestPath);
                
                // Create the new symlink with the proper directory.
                _symlink.Environment.Create(latestPath, targetDirectory);

                // Restart the service.
                // TODO: Overview the instllation scripts and make sure they use the latest directory
                // TODO: Talk to paul about how to go about restarting the service
                // TODO: Assuming there's some form of watchdog, should it restart automatically if we kill the application?
            }
        }

        /// <summary>
        /// Get the latest release information from Spectero's Servers.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InternalError"></exception>
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

        /// <summary>
        /// Get the root installation directory for the spectero daemons.
        /// This will inclue each versions and the latest symlink.
        /// </summary>
        private string RootInstallationDirectory => Directory.GetParent(
            Directory.GetParent(Program.GetAssemblyLocation()).FullName
        ).FullName;

        /// <summary>
        /// Get the directory of the database.
        /// </summary>
        private string DatabaseDirectory => Path.Combine(Program.GetAssemblyLocation(), _config.DatabaseDir);

        /// <summary>
        /// Get the paths for each database.
        /// </summary>
        /// <returns></returns>
        private string[] GetDatabasePaths() => Directory.GetFiles(DatabaseDirectory, "*.sqlite", SearchOption.TopDirectoryOnly);
    }
}