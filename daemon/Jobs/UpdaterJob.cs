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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using Medallion.Shell;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Readers;
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

        // Constants
        private static readonly string[] _validReleaseChannels =
        {
            "alpha", "stable", "beta"
        };

        // Updater Variables
        private string runningBranch;
        private string remoteVersion;
        private string remoteBranch;
        private string newVersion;
        private string targetDirectory;
        private string targetArchive;
        private string newDotnetCorePath;
        private string updateMessage;

        // Source Information from external properties
        private Release releaseInformation;
        private SourcesInformation sourcesInformation;


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
            // Check to see if we can enable the application
            if (!IsEnabled())
            {
                _logger.LogError(
                    "UJ: Job enabled, but matching criterion does not match. This should not happen, silently going back to sleep.");
                return;
            }

            // Check to see if there's already an update in progress
            if (AppConfig.UpdateDeadlock)
            {
                _logger.LogWarning("UJ: Update deadlock detected - there is already an update in progress.");
                return;
            }

            // Enable the deadlock.
            AppConfig.UpdateDeadlock = true;

            // Get the latest set of release data.
            releaseInformation = GetReleaseInformation();
            
            // Determine the release channel 
            runningBranch = _config.Updater.ReleaseChannel ?? AppConfig.ReleaseChannel;
            _logger.LogDebug("The running branch is " + runningBranch);

            // Validate that the release channel exists.
            if (!_validReleaseChannels.Contains(runningBranch))
            {
                // Let the user know
                _logger.LogWarning("UJ: The release channel {0} does not support updates - updating will be disabled.",
                    runningBranch);

                // Disable updating
                _config.Updater.Enabled = false;

                // Stop execution
                return;
            }

            // Get the remainder versioning information
            remoteVersion = releaseInformation.channels[runningBranch];
            remoteBranch = remoteVersion.Split("-")[1];

            // Compare and make sure there's an update available.
            if (remoteBranch == runningBranch && SemanticVersionUpdateChecker(remoteVersion))
            {
                // Update available.
                _logger.LogDebug("UJ: Generating target paths...");
                newVersion = releaseInformation.channels[remoteBranch];
                targetDirectory = Path.Combine(RootInstallationDirectory, newVersion);
                targetArchive = Path.Combine(RootInstallationDirectory, string.Format("{0}.zip", newVersion));
                _logger.LogDebug("UJ: Target paths generated successfully.");

                // Generate a projected path of where the new dotnet core installation should exist - will utilize this in the future.
                newDotnetCorePath = Path.Combine(targetDirectory, "dotnet");
                _logger.LogDebug("UJ: New Dotnet Core path generated: " + newDotnetCorePath);

                // Check if the target directory already exists, we will use this to determine if an update has already happened.
                if (Directory.Exists(targetDirectory))
                {
                    _logger.LogDebug("UJ: Target installation directory exists, thus the update will be skipped.");
                    /*
                     * Explanation
                     * There's already the directory for the updated version, at this point we should recognize we cannot modify it. 
                     */
                    AppConfig.UpdateDeadlock = false;
                    return;
                }

                // If we've made it this far without resetting the deadlock we can likely update.
                // Go ahead and print the update message.
                _logger.LogInformation(updateMessage);

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
                    var msg = "A error occured while attempting to resolve the download link for the update: \n" +
                              exception;
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
                        var msg = "UJ: The update job has failed due to a problem while downloading the update:\n" +
                                  exception;
                        _logger.LogError(msg);
                        AppConfig.UpdateDeadlock = false;
                        throw new InternalError(msg);
                    }
                }

                // Extract
                _logger.LogInformation("UJ: Extracting {0} to {1}", targetArchive, targetDirectory);
                ZipFile.ExtractToDirectory(targetArchive, targetDirectory);
                _logger.LogDebug("UJ: Archive has been extracted successfully.");

                // Delete the archive after extraction.
                File.Delete(targetArchive);
                _logger.LogDebug("UJ: Downloaded version archive has been deleted.");

                // Get the expected symlink path.
                var latestPath = Path.Combine(RootInstallationDirectory, "latest");
                _logger.LogDebug("UJ: Latest symlink path has been generated.");

                // Copy the databases
                _logger.LogDebug("UJ: Getting database migration information.");
                var databasePaths = GetDatabasePaths();
                _logger.LogDebug("UJ: {0} database(s) need to be migrated.", databasePaths.Length);
                foreach (var databasePath in GetDatabasePaths())
                {
                    try
                    {
                        var basename = new FileInfo(databasePath).Name;
                        var databaseDestinationPath = Path.Combine(targetDirectory, "daemon", "Database", basename);
                        _logger.LogDebug("UJ: Attempting to copy database {0} to path {1}", databasePath,
                            databaseDestinationPath);
                        File.Copy(databasePath, databaseDestinationPath);
                        _logger.LogInformation("UJ: Migrated Database: {0} => {1}", databasePath,
                            databaseDestinationPath);
                    }
                    catch (Exception exception)
                    {
                        AppConfig.UpdateDeadlock = false;
                        var msg =
                            "UJ: A exception occured while trying to migrate a database to the new destination:\n" +
                            exception;
                        _logger.LogError(msg);
                        throw exception;
                    }
                }


                // Dotnet Core Shenanigans
                _logger.LogDebug("UJ: Checking for dotnet core compatibility.");

                // Dotnet core placeholder variables.
                var dotnetCorePath = "";
                var dotnetCoreBinary = "";
                bool forceDownloaded = false;

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
                    var localDotnetCorePath = Path.Combine(latestPath, "dotnet");
                    var localDotnetCoreBinary =
                        Path.Combine(dotnetCorePath, "dotnet"); // Reminder: ./dotnet is an executable.

                    if (File.Exists(localDotnetCoreBinary))
                    {
                        // Assign the paths to the parent variables.
                        dotnetCorePath = localDotnetCorePath;
                        dotnetCoreBinary = localDotnetCoreBinary;
                    }
                    else
                    {
                        forceDownloaded = true;
                        DownloadDotnetCoreFramework();
                        _logger.LogInformation("UJ: Requirement for dotnet core has been satisfied by force download.");
                    }
                }


                // Compare dotnet versions if the variables are defined.
                if (dotnetCorePath != "" && dotnetCoreBinary != "" && forceDownloaded == false)
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
                    _logger.LogDebug("UJ: Executing dotnet command to get available runtimes.");
                    try
                    {
                        // Try to run the command and wait
                        var proc = _processRunner.Run(procOption);
                        proc.Command.Wait();

                        // Rubber duck: check to see if we succeeded.
                        _logger.LogDebug(
                            "UJ: Dotnet command successfully exited - will now print and iterate through each available framework.");

                        // TODO: MAKE THIS MORE ROBUST AFTER TESTING.
                        // Read the lines to see if compatible. 
                        foreach (var line in proc.Command.StandardOutput.GetLines())
                        {
                            // Rubber ducking
                            _logger.LogDebug("UJ: Current line value: `{0}`", line);

                            // Get the line that contains a version.
                            var fixedLine = "";
                            if (line.Contains("Microsoft.AspNetCore.All"))
                            {
                                // Rubber ducking
                                _logger.LogDebug("UJ: Potentially found a framework that supports this version.");

                                // Single out the data from the installed version.
                                fixedLine = line.Remove(0, 25); // Remove the "Microsoft.AspNetCore.All"
                                fixedLine = fixedLine.Substring(0, 5); // Single out the version              

                                // Rubber ducking: printing the version of the fixedline
                                _logger.LogDebug("UJ: Truncated version string comes out to: " + fixedLine);

                                // Split 
                                string[] installed = fixedLine.Split('.');
                                string[] requirement = releaseInformation.versions[remoteVersion]
                                    .requiredDotnetCoreVersion.Split('.');

                                // Compare versioning.
                                for (var i = 0; i != installed.Length; i++)
                                {
                                    // Rubber ducking: visual version comparison.
                                    _logger.LogDebug(
                                        "UJ: Dotnet Core Version Comparison index {0} has values {1} for installed, and the requirement is {2}",
                                        i, installed[i], requirement[i]);

                                    if (int.Parse(installed[i]) < int.Parse(requirement[i]))
                                    {
                                        // Rubber ducking: Invalid version
                                        _logger.LogDebug("UJ: The installed version of dotnet core is incompatible.");

                                        // The installed version cannot run the required version.
                                        // Download a new dotnet core.
                                        DownloadDotnetCoreFramework();
                                        goto loop_end;
                                    }
                                }

                                // Rubber Duck: 
                                _logger.LogDebug(
                                    "UJ: The dotnet core version comparison loop did not signify that the version was incompatible.");

                                // If the for loop above did nothing, we should be compatible.
                                break;
                            }
                        }

                        loop_end: ;
                        _logger.LogInformation("UJ: Requirement for dotnet core has been satisfied.");
                    }
                    catch (Exception exception)
                    {
                        AppConfig.UpdateDeadlock = false;
                        var msg =
                            "UJ: A exception occured while trying to validate a compatible dotnet core version:\n" +
                            exception;
                        _logger.LogError(msg);
                        throw exception;
                    }
                }
                else
                {
                    if (AppConfig.isWindows)
                    {
                        // The framework does not exist, download it.
                        DownloadDotnetCoreFramework();
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
                    var msg = "Failed to create symlink, Marshal response: " +
                              MarshalUtil.DecodeIntToString(Marshal.GetLastWin32Error());
                    _logger.LogError(msg);
                }

                // Restart the service.
                // We'll rely on the service manager to start us back up.
                _logger.LogInformation(
                    "The update process is complete, and the spectero service has been configured to run the latest version.\n" +
                    "Please restart the spectero service to utilize the latest Version.\n" +
                    "The application will now shutdown.");
                _applicationLifetime.StopApplication();
            }
            else
            {
                // Developer note: No update available!
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
            string releaseServer = "https://c.spectero.com/releases.json";
            try
            {
                var response = _httpClient.GetAsync(releaseServer).Result;
                var releaseData = JsonConvert.DeserializeObject<Release>(response.Content.ReadAsStringAsync().Result);
                return releaseData;
            }
            catch (Exception exception)
            {
                var msg = $"UJ: Failed to get release data from the internet ({releaseServer}).\n" + exception;
                _logger.LogError(msg);
                AppConfig.UpdateDeadlock = false;
                throw exception;
            }
        }

        /// <summary>
        /// Get download information from the sources repository on github.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InternalError"></exception>
        private SourcesInformation GetSources()
        {
            try
            {
                var response = _httpClient
                    .GetAsync("https://raw.githubusercontent.com/ProjectSpectero/daemon-installers/master/SOURCES.json")
                    .Result;

                var sourcesData =
                    JsonConvert.DeserializeObject<SourcesInformation>(response.Content.ReadAsStringAsync().Result);
                return sourcesData;
            }
            catch (Exception exception)
            {
                var msg = "UJ: Failed to get source information from github.\n" + exception;
                _logger.LogError(msg);
                AppConfig.UpdateDeadlock = false;
                throw exception;
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
        private string[] GetDatabasePaths() =>
            Directory.GetFiles(DatabaseDirectory, "*.sqlite", SearchOption.TopDirectoryOnly);

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
            try
            {
                // Divide by the dash/hyphen, then separate each version.
                string[] splitRemote = remote.Split("-")[0].Split(".");

                // Check for semantic versioning differences.
                if (splitRemote.Length == 3 && AppConfig.Version.Split(".").Length == 2)
                {
                    _logger.LogWarning(
                        "The latest release is semantic versioned while the current running version release is not - an update will be forced.");
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
                    updateMessage = ("There is a new major release available for the Spectero Daemon.");
                    return true;
                }

                // Compare the MINOR level of semantic versioning.
                if (int.Parse(splitRemote[1]) > AppConfig.MinorVersion)
                {
                    updateMessage = ("There is a new minor release available for the Spectero Daemon.");
                    return true;
                }

                // Compare the PATCH level of semantic versioning.
                if (int.Parse(splitRemote[2]) > AppConfig.PatchVersion)
                {
                    updateMessage = ("There is a new patch available for the Spectero Daemon.");
                    return true;
                }

                // Generic return, no update available although we should never reach here.
                return false;
            }
            catch (Exception exception)
            {
                AppConfig.UpdateDeadlock = false;
                var msg = "UJ: A exception occured while trying to parse semantic versioning\n" + exception;
                _logger.LogError(msg);
                throw exception;
            }
        }


        /// <summary>
        /// Install the latest version of the dotnet core framework
        ///
        /// Windows: We use the installer and the native environment variable as this is streamlined.
        /// Linux: We use a local installation in the relative directory of the daemon.
        /// </summary>
        /// <exception cref="ErrorExitCodeException"></exception>
        public void DownloadDotnetCoreFramework()
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
                    Arguments = new[] {"/install", "/passive", "/norestart", "/q"},
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
                    throw exception;
                }
            }
            else if (AppConfig.isUnix)
            {
                var cpuArchitecture = _architectureUtility.GetArchitecture();
                _logger.LogDebug("UJ: Linux specific command - determined architecture is {0}", cpuArchitecture);

                // Generate path of where zip should be saved.
                var downloadPath = Path.Combine(targetDirectory, string.Format("dotnet-{0}.tar.gz", cpuArchitecture));

                // Download zip
                using (var client = new WebClient())
                {
                    client.DownloadFile(sources.dependencies.dotnet[
                        releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion
                    ].linux[cpuArchitecture], downloadPath);

                    // Let the console know we've downloaded the archive
                    _logger.LogInformation("Successfully downloaded dotnet core runtime {0} archive.",
                        releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion);
                }


                // Create the dotnet directory.
                if (!Directory.Exists(newDotnetCorePath)) Directory.CreateDirectory(newDotnetCorePath);

                // Extract
                using (Stream stream = File.OpenRead(downloadPath))
                {
                    var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory) reader.WriteEntryToDirectory(newDotnetCorePath);
                    }

                    // tell the console that extraction was successful.
                    _logger.LogInformation("UJ: Dotnet Core {0} has been extracted successfully.",
                        releaseInformation.versions[remoteVersion].requiredDotnetCoreVersion);
                }

                // Delete archive
                File.Delete(downloadPath);
                _logger.LogDebug("UJ: Cleanup successful.");
            }
        }
    }
}