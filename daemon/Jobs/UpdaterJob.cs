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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Marhsal;
using Spectero.daemon.Libraries.Symlink;

namespace Spectero.daemon.Jobs
{
    /// <summary>
    /// Configuration blueprint that the appconfig will provide.
    /// </summary>
    public class UpdaterConfiguration
    {
        // public string ReleaseChannel { get; set; }
        public bool Enabled { get; set; }
        public string ReleaseChannel { get; set; }
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
            if (!IsEnabled())
            {
                _logger.LogError("UJ: Job enabled, but matching criterion does not match. This should not happen, silently going back to sleep.");
                return;
            }

            if (AppConfig.UpdateDeadlock == true)
            {
                _logger.LogWarning("UJ: Update deadlock detected - there is already an update in progress.");
                return;
            }

            // Enable the deadlock.
            AppConfig.UpdateDeadlock = true;

            // Get the latest set of release data.
            var releaseInformation = GetReleaseInformation();

            // Get Version details.
            var runningBranch = _config.Updater.ReleaseChannel ?? AppConfig.ReleaseChannel;
            var remoteVersion = releaseInformation.channels[runningBranch];
            var remoteBranch = remoteVersion.Split("-")[1];


            // Compare
            if (remoteBranch == runningBranch && SemanticVersionUpdateChecker(remoteVersion))
            {
                // Update available.
                var newVersion = releaseInformation.channels[remoteBranch];
                var targetDirectory = Path.Combine(RootInstallationDirectory, newVersion);
                var targetArchive = Path.Combine(RootInstallationDirectory, string.Format("{0}.zip", newVersion));

                // Check if the target directory already exists, we will use this to determine if an update has already happened.
                if (Directory.Exists(targetDirectory))
                {
                    AppConfig.UpdateDeadlock = false;
                    return;
                }

                // Log to the console that the update is available.
                _logger.LogInformation("UJ: There is a update available for the spectero daemon: " + newVersion);

                // Get the download link
                string downloadLink = null;
                try
                {
                    downloadLink = releaseInformation.versions[newVersion].download;
                }
                catch (Exception exception)
                {
                    var msg = "A error occured while attemting to resolve the download link for the update: \n" + exception;
                    _logger.LogError(msg);
                    AppConfig.UpdateDeadlock = false;
                    throw new InternalError(msg);
                }

                // Download
                using (var webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadFile(new Uri(downloadLink), targetArchive);
                        _logger.LogInformation("UJ: Update {0} has been downloaded successfully.", newVersion);
                    }
                    catch (WebException exception)
                    {
                        var msg = "The update job has failed due to a problem while downloading the update: " + exception;
                        _logger.LogError(msg);
                        AppConfig.UpdateDeadlock = false;
                        throw new InternalError(msg);
                    }
                }

                // Extract
                _logger.LogInformation("UJ: Extracting {0} to {1}", targetArchive, targetDirectory);
                ZipFile.ExtractToDirectory(targetArchive, targetDirectory);

                // Delete the archive after extraction.
                File.Delete(targetArchive);
                
                
                //TODO: COPY DOTNET RUNTIMES.
                if ()

                // Copy the databases
                foreach (var databasePath in GetDatabasePaths())
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
                if (_symlink.Environment.Create(latestPath, targetDirectory))
                    _logger.LogDebug("UJ: Created Symbolic Link: {0}->{1}", latestPath, targetDirectory);
                else
                {
                    var msg = "Failed to create symlink, Marshal response: " + MarshalUtil.DecodeIntToString(Marshal.GetLastWin32Error());
                    _logger.LogError(msg);
                }

                // Restart the service.
                // We'll rely on the service manager to start us back up.
                _logger.LogInformation("The update process is complete, and the spectero service has been configured to run the latest Version.\n" +
                                       "Please restart the spectero service to utilize the latest Version.\n" +
                                       "The application will now shutdown.");
                _applicationLifetime.StopApplication();
            }

            // Disable th deadlock and allow the next run.
            AppConfig.UpdateDeadlock = false;
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
                AppConfig.UpdateDeadlock = false;
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

        /// <summary>
        /// Comparison function designed specifically for semantic versioning.
        /// Ths function will handle every possible edge case for a different version, and return a bool if there's an update available
        /// based on the data provided.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="running"></param>
        /// <returns></returns>
        private bool SemanticVersionUpdateChecker(string remote)
        {
            string[] splitRemote = remote.Split(".");

            // Check for semantic versioning differences.
            if (splitRemote.Length == 3 && AppConfig.Version.Split(".").Length == 2)
            {
                _logger.LogWarning("The latest release is semantic versioned while the current running version release is not - an update will be forced.");
                return true;
            }
            // We're already running the latest version.
            else if (remote == AppConfig.Version)
            {
                return false;
            }

            // Compare the MAJOR level of semantic versioning.
            if (int.Parse(splitRemote[0]) > AppConfig.MajorVersion)
            {
                _logger.LogInformation("There is a new major release available for the Spectero Daemon.");
                return true;
            }

            // Compare the MINOR level of semantic versioning.
            if (int.Parse(splitRemote[1]) > AppConfig.MinorVersion)
            {
                _logger.LogInformation("There is a new minor release available for the Spectero Daemon.");
                return true;
            }

            // Compare the PATCH level of semantic versioning.
            if (int.Parse(splitRemote[2]) > AppConfig.PatchVersion)
            {
                _logger.LogInformation("There is a new patch available for the Spectero Daemon.");
                return true;
            }

            // Generic return, no update available although we should never reach here.
            return false;
        }
    }
}