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
using Medallion.Shell;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Marhsal;
using Spectero.daemon.Libraries.Symlink;
using Spectero.daemon.Libraries.Utilities.Architecture;

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

    public class DotnetVersion
    {
        public Dictionary<string, string> linux;
        public Dictionary<string, string> windows;
    }

    public class SourcesDependenciesProperty
    {
        public Dictionary<string, DotnetVersion> dotnet;
        public string nssm { get; set; }
    }

    public class SourcesInformation
    {
        [JsonProperty(PropertyName = "terms-of-service")]
        public string termsOfService { get; set; }

        public SourcesDependenciesProperty dependencies { get; set; }
    }


    public class UpdaterJob : IJob
    {
        // Class Dependencies
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdaterJob> _logger;
        private readonly AppConfig _config;
        private readonly ISymlink _symlink;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly IArchitectureUtility _architectureUtility;
        private readonly IProcessRunner _processRunner;
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
            ISymlink symlink,
            IApplicationLifetime applicationLifetime,
            IProcessRunner processRunner,
            IArchitectureUtility architectureUtility)
        {
            _httpClient = httpClient;
            
            // Set symlink inheritance - also needs the processrunner.
            _symlink = symlink;
            _symlink.SetProcessRunner(processRunner);
            
            // Inherit the process runner into the class for when needed.
            _processRunner = processRunner;
            
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _architectureUtility = architectureUtility;
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

                // Generate a projected path of where the new dotnet core installation should exist - will utilize this in the future.
                var newDotnetCorePath = Path.Combine(targetDirectory, "dotnet");

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

                // Get the expected symlink path.
                var latestPath = Path.Combine(RootInstallationDirectory, "latest");

                // Copy the databases
                foreach (var databasePath in GetDatabasePaths())
                {
                    var basename = new FileInfo(databasePath).Name;
                    var databaseDestPath = Path.Combine(targetDirectory, "daemon", "Database", basename);
                    File.Copy(databasePath, databaseDestPath);
                    _logger.LogInformation("UJ: Migrated Database: {0} => {1}", databasePath, databaseDestPath);
                }


                // Dotnet Core Shenanigans
                _logger.LogDebug("UJ: Checking for dotnet core compatibility.");

                // Dotnet core placeholder variables.
                var dotnetCorePath = "";
                var dotnetCoreBinary = "";
                if (AppConfig.isWindows)
                {
                    // Potential windows directory where files exist.
                    var potentialDotnetDirectories = new string[]
                    {
                        "C:/Program Files/dotnet/dotnet.exe",
                        "C:/Program Files (x86)/dotnet/dotnet.exe",
                        Path.Combine(latestPath, "dotnet", "dotnet.exe")
                    };

                    // Iterate through each possibility
                    foreach (var iterPath in potentialDotnetDirectories)
                    {
                        // Check to see if it exists
                        if (File.Exists(iterPath))
                        {
                            // Assign the paths to the parent variables.
                            dotnetCorePath = new FileInfo(iterPath).Directory.FullName;
                            dotnetCoreBinary = iterPath;
                        }

                        // If none of the paths are valid, we shall install - exit point
                        break;
                    }
                }
                else if (AppConfig.isUnix)
                {
                    // We only perform local installs on linux, and thus the currently running version should have a copy - local variables that can be easily disposed.
                    var _dotnetCorePath = Path.Combine(latestPath, "dotnet");
                    var _dotnetCoreBinary = Path.Combine(dotnetCorePath, "dotnet"); // Reminder: ./dotnet is an executable.

                    if (File.Exists(_dotnetCoreBinary))
                    {
                        // Assign the paths to the parent variables.
                        dotnetCorePath = _dotnetCorePath;
                        dotnetCoreBinary = _dotnetCoreBinary;
                    }
                }


                // Compare dotnet versions.
                if (dotnetCorePath != "" && dotnetCoreBinary != "")
                {
                    // Exists, see what it's got.
                    _logger.LogDebug("UJ: Found dotnet core installation: " + dotnetCoreBinary);

                    // Determine if it can be used.
                    // Create the process options
                    var procOption = new ProcessOptions()
                    {
                        Executable = dotnetCoreBinary,
                        WorkingDirectory = dotnetCorePath,
                        Arguments = new string[] {"--list-runtimes"},
                        Monitor = false
                    };

                    // Run the event.
                    var proc = _processRunner.Run(procOption);

                    // TODO: MAKE THIS MORE ROBUST AFTER TESTING.
                    // Read the lines to see if compatible. 
                    foreach (var line in proc.Command.StandardOutput.ReadToEnd().Split("\n"))
                    {
                        // Get the line that contains a version.
                        var fixedLine = "";
                        if (line.Contains("Microsoft.AspNetCore.All"))
                        {
                            // Single out the data from the installed version.
                            fixedLine = line.Remove(0, 25); // Remove the "Microsoft.AspNetCore.All"
                            fixedLine = fixedLine.Substring(0, 5); // Single out the version                

                            // Split 
                            string[] installed = fixedLine.Split('.');
                            string[] requirement = releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion.Split('.');

                            // Compare.
                            for (var i = 0; i != installed.Length; i++)
                            {
                                if (int.Parse(installed[i]) < int.Parse(requirement[i]))
                                {
                                    // The installed version cannot run the required version.
                                    // Download a new dotnet core.
                                }
                            }

                            // Get the projected path of where dotnet core should be copied - create the directory if it does not exist..
                            if (Directory.Exists(newDotnetCorePath)) Directory.CreateDirectory(newDotnetCorePath);

                            // Create the required directories
                            foreach (var dirPath in Directory.GetDirectories(dotnetCorePath, "*", SearchOption.AllDirectories))
                                Directory.CreateDirectory(dirPath.Replace(dotnetCorePath, newDotnetCorePath));

                            // Copy the files.
                            foreach (var newPath in Directory.GetFiles(dotnetCorePath, "*.*", SearchOption.AllDirectories))
                                File.Copy(newPath, newPath.Replace(dotnetCorePath, newDotnetCorePath), true);

                            _logger.LogInformation("UJ: Requirement for dotnet core has been satisfied.");
                        }
                    }
                }
                else
                {
                    // Doesn't exist, download a new version.
                    _logger.LogDebug("UJ: A usable dotnet core installation was not found, thus will be made.");

                    // Get Sources.json information.
                    var sources = GetSources();

                    // Determine operating system
                    if (AppConfig.isWindows)
                    {
                        // Download installer
                        var downloadPath = Path.Combine(targetDirectory, "dotnet-installer.exe");
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(sources.dependencies.dotnet[
                                releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion
                            ].windows["default"], downloadPath);
                        }

                        // Create a process runner for the new dotnet installer
                        var dotnetInstallerProcOptions = new ProcessOptions()
                        {
                            Executable = downloadPath,
                            InvokeAsSuperuser = true,
                            Monitor = false
                        };

                        // Attempt to run the installer.
                        try
                        {
                            // Run the installer.
                            var installerRunner = _processRunner.Run(dotnetInstallerProcOptions);
                            installerRunner.Command.Wait();

                            // If we can get past the wait, the installation succeeded.
                            _logger.LogInformation("UJ: Dotnet Core Runtime was successfully updated to version {0}",
                                releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion);
                        }
                        catch (ErrorExitCodeException exception)
                        {
                            AppConfig.UpdateDeadlock = false;
                            var msg = "UJ: A exception occured while trying to update dotnet core for windows\n" + exception;
                            _logger.LogError(msg);
                            throw new Exception(msg);
                        }
                    }
                    else if (AppConfig.isUnix)
                    {
                        // Generate path of where zip should be saved.
                        var downloadPath = Path.Combine(targetDirectory, "dotnet.zip");

                        // Download zip
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(sources.dependencies.dotnet[
                                releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion
                            ].linux[_architectureUtility.GetArchitecture()], downloadPath);
                        }

                        // Extract
                        ZipFile.ExtractToDirectory(downloadPath, newDotnetCorePath);

                        // Delete zip
                        File.Delete(downloadPath);
                    }
                }


                // Delete the symlink if it exists.
                if (_symlink.IsSymlink(latestPath))
                {
                    _symlink.GetEnvironment().Delete(latestPath);
                    _logger.LogDebug("UJ: Deleted old Symbolic Link: " + latestPath);
                }

                // Create the new symlink with the proper directory.
                if (_symlink.GetEnvironment().Create(latestPath, targetDirectory))
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

            // Disable the deadlock and allow the next run.
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

        private SourcesInformation GetSources()
        {
            try
            {
                var response = _httpClient.GetAsync("https://raw.githubusercontent.com/ProjectSpectero/daemon-installers/master/SOURCES.json").Result;

                var sourcesData = JsonConvert.DeserializeObject<SourcesInformation>(response.Content.ReadAsStringAsync().Result);
                return sourcesData;
            }
            catch (Exception exception)
            {
                var msg = "UJ: Failed to get source information from github.\n" + exception;
                _logger.LogError(msg);
                AppConfig.UpdateDeadlock = false;
                throw new InternalError(msg);
            }
        }

        /// <summary>
        /// Get the root installation directory for the spectero daemons.
        /// This will include each versions and the latest symlink.
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