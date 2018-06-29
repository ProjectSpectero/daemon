using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Firewall;
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
        private readonly Firewall _firewall;
        private readonly List<string> _configsOnDisk;

        // Class variables that will be modified.
        private ServiceState _state = ServiceState.Halted;
        private IEnumerable<OpenVPNConfig> _vpnConfig;
        

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

            // Invoke the firewall, talk to paul about this.
            _firewall = new Firewall(logger, _processRunner);
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

            // Get the default network interace.
            var defaultNetworkInterface = _firewall.GetDefaultInterface();

            // Context-aware dictionary of OpenVPN configs and their rendered forms.
            var configDictionary = new Dictionary<OpenVPNConfig, string>();

            foreach (var vpnConfig in _vpnConfig)
            {
                var renderedConfig = vpnConfig.GetStringConfig().Result;
                configDictionary.Add(vpnConfig, renderedConfig);
            }

            // Iterate through each pending configuration.
            foreach (var configHolder in configDictionary)
            {
                // Get the properly formatted path of where the configuration will be stored..
                var onDiskName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ovpn");

                // Write the configuration to the disk.
                using (var writer = new StreamWriter(onDiskName))
                    writer.Write(configHolder.Value);

                // Keep track of the configuration path.
                _configsOnDisk.Add(onDiskName);

                // Log to the console.
                _logger.LogDebug($"OpenVPN init: wrote config to {onDiskName}, attempting to start 3rd party daemon.");

                // At this stage, we have the configs ready and on disk. Let us simply bootstrap the processes.
                StartDaemon(onDiskName);
 
                _firewall.Rules.Masquerade(configHolder.Key.Listener.Network, defaultNetworkInterface.Name);
            }
        }

        /// <summary>
        /// Determine the absolute path of OpenVPN.
        /// </summary>
        /// <returns></returns>
        private string DetermineBinaryPath()
        {
            // Placeholder varaiuble to store the path.
            string binaryPath = null;

            // Check Linux/OS X Operating system for OpenVPN Installation.
            if (AppConfig.isUnix)
            {
                // Attempt to properly find the path of OpenVPN
                try
                {
                    var whichArray = new[] {"which", "openvpn"};
                    var whichFinder = Medallion.Shell.Command.Run("sudo", whichArray);

                    // Parse the output and get the absolute path.
                    var ovpnPath = whichFinder.StandardOutput.GetLines().ToList()[0];
                    _logger.LogDebug("OpenVPN was found: {0}", ovpnPath);
                    binaryPath = ovpnPath;
                }
                catch (Exception)
                {
                    // OpenVPN wasn't found.
                }
            }
            // Windows - Check Program Files Installations.
            else if (AppConfig.isWindows)
            {
                // Potential installation paths of OpenVPN.
                string[] potentialOpenVpnPaths =
                {
                    "C:\\Program Files (x86)\\OpenVPN\\bin\\openvpn.exe",
                    "C:\\Program Files\\OpenVPN\\bin\\openvpn.exe",
                };

                // Iterate through each potential path and find what exists.
                foreach (var currentOpenVpnPath in potentialOpenVpnPaths)
                {
                    if (!File.Exists(currentOpenVpnPath)) continue;
                    _logger.LogDebug("OpenVPN was found: {0}", currentOpenVpnPath);
                    binaryPath = currentOpenVpnPath;
                    break;
                }
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "OpenVPN: This daemon does not know how to initialize OpenVPN on this platform."
                );
            }

            CheckBinaryPath(binaryPath);

            // Return the found path.
            return binaryPath;
        }

        private void CheckBinaryPath(string binaryPath)
        {
            if (binaryPath.IsNullOrEmpty())
            {
                _logger.LogError(
                    "OpenVPN init: we couldn't find the OpenVPN binary. Please make sure it is installed " +
                    "(for Unix: use your package manager), for Windows: download and install the binary distribution."
                );

                // TODO: Dress this up properly to make disclosing just what the hell went wrong easier.
                throw new EInternalError();
            }
        }

        /// <summary>
        /// Start the daemon
        ///
        /// Now, we call this with MedallionShell and store the command descriptor.
        /// Before Startup, we need to set the effective working directory to "3rdParty/OpenVPN"
        /// </summary>
        /// <param name="configPath"></param>
        private void StartDaemon(string configPath) =>
            _processRunner.Run(
                // Instructions
                new ProcessOptions()
                {
                    Executable = DetermineBinaryPath(),
                    Arguments = new[] {configPath},
                    Daemonized = true,
                    Monitor = true,
                    DisposeOnExit = false,
                    InvokeAsSuperuser = true,
                    WorkingDirectory = Path.Combine(Program.GetAssemblyLocation(), "3rdParty/OpenVPN")
                },
                // The calling object.
                this
            );

        /// <summary>
        /// Start the OpenVPN Service from the provided List{IServiceConfig}.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            LogState("Stop");
            
            var allowedStates = new[] {ServiceState.Halted, ServiceState.Restarting};
            if (! allowedStates.Any(x => x == _state))
            {
                _logger.LogError($"OpenVPN: cowardly refusing start because current state ({_state}) does not allow OpenVPN startup.");
                return;
            }
            
            _state = ServiceState.Running;
            Initialize(serviceConfig);

            LogState("Stop");
        }
            

        /// <summary>
        /// Restart the OpenVPN Service.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            SetConfig(serviceConfig);
            _state = ServiceState.Restarting;
            Stop();
            Start();
        }


        /// <summary>
        /// Effectively similar to restart in this context.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void Reload(IEnumerable<IServiceConfig> serviceConfig = null) =>
            ReStart(serviceConfig);

        /// <summary>
        /// Stop the OpenVPN Service.
        /// Temporary configurations will also be deleted, and the tracking object will be emptied.
        /// </summary>
        public void Stop()
        {
            LogState("Stop");
            
            // We should probably think about if we want this to throw an exception instead, because the invocation would effectively be illegal at that stage.
            if (_state != ServiceState.Running) return;
            
            // Stop all running configurations
            _processRunner.CloseAllBelongingToService(this);

            // Iterate through each configuration on the disk.
            foreach (var fileOnDisk in _configsOnDisk)
                File.Delete(fileOnDisk);

            // Clear the list of configurations on the disk.
            _configsOnDisk.Clear();

            _state = ServiceState.Halted;
            
            LogState("Stop");
        }

        /// <summary>
        /// Get the state of the service.
        /// </summary>
        /// <returns></returns>
        public ServiceState GetState() => _state;

        /// <summary>
        /// Get the list of configurations.
        /// The _vpnConfig is a private class variable, and this should be considered as a getter.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IServiceConfig> GetConfig() =>
            _vpnConfig;

        /// <summary>
        /// TODO: IMPLEMENT THIS FUNCTION.
        /// Get the state of the logger.
        /// </summary>
        /// <param name="caller"></param>
        public void LogState(string caller) =>
            _logger.LogDebug($"OpenVPN ({caller}): current state is {_state}");

        /// <summary>
        /// Apply a list of configurations to this instance of the OpenVPN Class.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="restartNeeded"></param>
        public void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            if (config != null)
            {
                _vpnConfig = config as List<OpenVPNConfig>;
                if (restartNeeded)
                    ReStart();
            }
                
        }
            
    }
}