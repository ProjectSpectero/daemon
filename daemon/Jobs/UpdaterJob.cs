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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Symlink;

namespace Spectero.daemon.Jobs
{
    /// <summary>
    /// Configuration blueprint that the appconfig will provide.
    /// </summary>
    public class UpdaterConfiguration
    {
        public string ReleaseChannel { get; set; }
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
        private readonly IApplicationLifetime _applicationLifetime;
        private JObject _releaseInformation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configMonitor"></param>`
        /// <param name="logger"></param>
        /// <param name="applicationLifetime"></param>
        /// <param name="processRunner"></param>
        public UpdaterJob
        (IOptionsMonitor<AppConfig> configMonitor,
            ILogger<UpdaterJob> logger,
            HttpClient httpClient,
            Symlink symlink,
            IApplicationLifetime applicationLifetime,
            IProcessRunner processRunner)
        {
            _httpClient = httpClient;
            _symlink = symlink;
            _symlink.processRunner = processRunner;
            _applicationLifetime = applicationLifetime;
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
                var targetArchive = Path.Combine(RootInstallationDirectory, string.Format("{0}.zip", newVersion));
                
                // Check if the target directory already exists, we will use this to determine if an update has already happened.
                if (Directory.Exists(targetDirectory)) return;;

                // Log to the console that the update is available.
                _logger.LogInformation("UJ: There is a update available for the spectero daemon: " + newVersion);

                // Get the download link
                var downloadLink = releaseInformation.versions[newVersion].download;

                // Download
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadFile(new Uri(downloadLink), targetArchive);
                        _logger.LogInformation("UJ: Update {0} has been downloaded successfully.", newVersion);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("The update job has failed due to a problem while downloading the update: " + exception);
                    }
                }
                    

                // Extract
                ZipFile.ExtractToDirectory(targetArchive, targetDirectory);
                _logger.LogInformation("UJ: Extracting {0} to {1}", targetArchive, targetDirectory);

                // Copy the databases
                foreach (string databasePath in GetDatabasePaths())
                {
                    var basename = new FileInfo(databasePath).Name;
                    var databaseDestPath = Path.Combine(targetDirectory, "daemon", "Database", basename);
                    File.Copy(databasePath, databaseDestPath);
                    _logger.LogInformation("UJ: Migrated Database: {0} => {1}", databasePath, databaseDestPath);
                }

                // Get the expected symlink path.
                var latestPath = Path.Combine(RootInstallationDirectory, "latest");

                // Delete the symlink if it exists.
                if (_symlink.IsSymlink(latestPath))
                {
                    _symlink.Environment.Delete(latestPath);
                    _logger.LogDebug("UJ: Deleted old Symbolic Link: " + latestPath);
                }

                // Create the new symlink with the proper directory.
                //TODO: FIX - Symbolic Link Creation is broken for some reason!
                _symlink.Environment.Create(latestPath, targetDirectory);
                _logger.LogDebug("UJ: Created Symbolic Link: {0}->{1}", latestPath, targetDirectory);

                // Restart the service.
                // We'll rely on the service manager to start us back up.
                _logger.LogInformation("The update process is complete, and the spectero service has been configured to run the latest version.\n" +
                                       "Please restart the spectero service to utilize the latest version.\n" +
                                       "The application will now shutdown.");
                _applicationLifetime.StopApplication();
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