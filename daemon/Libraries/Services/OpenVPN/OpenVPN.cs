using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using Medallion.Shell;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPN : IService
    {
        // Dependency Injected Objects.
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly IEnumerable<IPAddress> _localAddresses;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IStatistician _statistician;
        private readonly IMemoryCache _cache;
        private readonly IProcessRunner _processRunner;
        private IEnumerable<OpenVPNConfig> _vpnConfig;

        // Class variables that will be modified.
        private readonly ServiceState State = ServiceState.Halted;
        private readonly List<string> _configsOnDisk;
        private readonly List<Command> _runningCommands;

        /// <summary>
        /// Standard Constructor
        /// Initializes the class without any form of dependency injection
        /// </summary>
        public OpenVPN()
        {
        }

        /// <summary>
        /// Dependency Injected Constructor
        /// Initializes the class with all of it's passed arguments.
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="logger"></param>
        /// <param name="db"></param>
        /// <param name="authenticator"></param>
        /// <param name="localNetworks"></param>
        /// <param name="localAddresses"></param>
        /// <param name="statistician"></param>
        /// <param name="cache"></param>
        /// <param name="processRunner"></param>
        public OpenVPN(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IEnumerable<IPAddress> localAddresses,
            IStatistician statistician, IMemoryCache cache,
            IProcessRunner processRunner)
        {
            // Inherit the objects.
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _statistician = statistician;
            _cache = cache;
            _localAddresses = localAddresses;
            _processRunner = processRunner;

            // This is tracked so we can clean it up when stopping (assuming managed stop).
            _configsOnDisk = new List<string>();
            _runningCommands = new List<Command>();
        }

        /// <summary>
        /// Initialize the class and configurations
        /// </summary>
        /// <param name="serviceConfigs"></param>
        private void Initialize(IEnumerable<IServiceConfig> serviceConfigs)
        {
            if (serviceConfigs != null)
                _vpnConfig = serviceConfigs as List<OpenVPNConfig>;

            // Check if configurations are passed.
            if (_vpnConfig == null || !_vpnConfig.Any())
                throw new InvalidOperationException("OpenVPN init: config list was null.");

            // Now, let's render the configurations into proper OpenVPN config files.
            var renderedConfigs = _vpnConfig.Select(x => x.GetStringConfig().Result);

            // Iterate through each pending configuration.
            foreach (var config in renderedConfigs)
            {
                // Get the properly formatted path of where the configuration will be stored..
                var onDiskName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ovpn");

                // Write the configuration to the disk.
                using (var writer = new StreamWriter(onDiskName))
                    writer.Write(config);

                // Keep track of the configuration path.
                _configsOnDisk.Add(onDiskName);

                // Log to the console.
                _logger.LogDebug($"OpenVPN init: wrote config to {onDiskName}, attempting to start 3rd party daemon.");

                // At this stage, we have the configs ready and on disk. Let us simply bootstrap the processes.
                StartDaemon(onDiskName);
            }

            // TODO: Invoke OpenVPN once per config on disk and track the process handle somewhere.
            // TODO: We also need to hook into netlink+netfilter (or its OS specific counterparts) to enable MASQUERADE/SNAT for our NATed IPs.
        }

        /// <summary>
        /// Determine the absolute path of OpenVPN.
        /// </summary>
        /// <returns></returns>
        private static string DetermineBinaryPath()
        {
            // Placeholder varaiuble to store the path.
            string binaryPath = null;

            // Check Linux/OS X Operating system for OpenVPN Installation.
            if (AppConfig.isUnix)
            {
                // Attempt to properly find the path of OpenVPN
                try
                {
                    var whichFinder = Command.Run("which", "openvpn");

                    // Parse the output and get the absolute path.
                    binaryPath = whichFinder.StandardOutput.GetLines().ToList()[0];
                }
                catch (Exception exception)
                {
                    // OpenVPN wasn't found.
                    throw new EInternalError();
                }
            }
            // Windows - Check Program Files Installations.
            else if (AppConfig.isWindows)
            {
                // Potential installation paths of OpenVPN.
                string[] potentialOpenVpnPaths =
                {
                    "C:\\Program Files (x86)\\OpenVPN\\bin\\OpenVPN.exe",
                    "C:\\Program Files\\OpenVPN\\bin\\OpenVPN.exe",
                };

                // Iterate through each potential path and find what exists.
                foreach (var currentOpenVpnPath in potentialOpenVpnPaths)
                    if (File.Exists(currentOpenVpnPath))
                        binaryPath = currentOpenVpnPath;

                // Check if we haven't found anything, if not throw an error.
                if (binaryPath == null) throw new EInternalError();
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "OpenVPN: This daemon does not know how to initialize OpenVPN on this platform."
                );
            }

            // Return the found path.
            return binaryPath;
        }

        /// <summary>
        /// Start the daemon
        ///
        /// Now, we call this with MedallionShell and store the command descriptor.
        /// Before Startup, we need to set the effective working directory to "3rdParty/OpenVPN"
        /// </summary>
        /// <param name="configPath"></param>
        private void StartDaemon(string configPath)
        {
            // Run the commmand
            var command = Command.Run(DetermineBinaryPath(), configPath);

            // Keep track of the running command
            _runningCommands.Add(command);
        }

        /// <summary>
        /// Start the OpenVPN Service from the provided List{IServiceConfig}.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            Initialize(serviceConfig);
        }

        /// <summary>
        /// Restart the OpenVPN Service.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            _vpnConfig = serviceConfig as List<OpenVPNConfig>;
        }

        /// <summary>
        /// TODO: IMPLEMENT THIS FUNCTION.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void Reload(IEnumerable<IServiceConfig> serviceConfig = null) =>
            new NotSupportedException();

        /// <summary>
        /// Stop the OpenVPN Service.
        /// Temporary configurations will also be deleted, and the tracking object will be emptied.
        /// </summary>
        public void Stop()
        {
            // Iterate through each configuration on the disk.
            foreach (var fileOnDisk in _configsOnDisk)
                File.Delete(fileOnDisk);

            // Clear the list of configurations on the disk.
            _configsOnDisk.Clear();
        }

        /// <summary>
        /// Get the state of the service.
        /// </summary>
        /// <returns></returns>
        public ServiceState GetState() => State;

        /// <summary>
        /// Get the list of configurations.
        /// The _vpnConfig is a private class variable, and this should be considered as a getter.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IServiceConfig> GetConfig() => _vpnConfig;

        /// <summary>
        /// TODO: IMPLEMENT THIS FUNCTION.
        /// Get the state of the logger.
        /// </summary>
        /// <param name="caller"></param>
        public void LogState(string caller) => new NotSupportedException();

        /// <summary>
        /// Apply a list of configurations to this instance of the OpenVPN Class.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="restartNeeded"></param>
        public void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false) =>
            _vpnConfig = config as List<OpenVPNConfig>;
    }
}