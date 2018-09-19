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
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Firewall;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPN : BaseService
    {
        // Dependency Injected Objects.

        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly IEnumerable<IPAddress> _localAddresses;
        private new readonly ILogger<OpenVPN> _logger;
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
        /// <param name="serviceProvider"></param>
        public OpenVPN(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Inherit the objects.
            _logger = serviceProvider.GetRequiredService<ILogger<OpenVPN>>();
            
            _processRunner = serviceProvider.GetRequiredService<IProcessRunner>();

            // This is tracked so we can clean it up when stopping (assuming managed stop).
            _configsOnDisk = new List<string>();

            // Invoke the firewall, talk to paul about this.
            _firewall = new Firewall(_logger, _processRunner);
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
                throw new InternalError("OpenVPN init: config list was null.");

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
                var pocoConfig = configHolder.Key;
                
                // Let's start the process off by registering the required port(s) on the port registry.
                // Possibility of lots of exceptions here, but that's alright. Anything failing should halt service startup.
                // ReSharper disable twice PossibleInvalidOperationException
                _portRegistry.Allocate(IPAddress.Parse(pocoConfig.Listener.IPAddress), (int) pocoConfig.Listener.Port,
                    (TransportProtocol) pocoConfig.Listener.Protocol, this);
                
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
                
                // Create MASQ rules for each config.
                _firewall.Rules.Masquerade(pocoConfig.Listener.Network, defaultNetworkInterface.Name);
            }
        }

        /// <summary>
        /// Determine the absolute path of OpenVPN.
        /// </summary>
        /// <returns></returns>
        private string DetermineBinaryPath()
        {
            // Placeholder variable to store the path.
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
                throw new InternalError();
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
                    MonitoringInterval = 10,
                    DisposeOnExit = false,
                    InvokeAsSuperuser = true,
                    AttachLogToConsole = true,
                    WorkingDirectory = Path.Combine(Program.GetAssemblyLocation(), "3rdParty/OpenVPN")
                },
                // The calling object.
                this
            );

        /// <summary>
        /// Start the OpenVPN Service from the provided List{IServiceConfig}.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public override void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            LogState("Start");
            
            var allowedStates = new[] {ServiceState.Halted, ServiceState.Restarting};
            if (! allowedStates.Any(x => x == _state))
            {
                _logger.LogError($"OpenVPN: cowardly refusing start because current state ({_state}) does not allow OpenVPN startup.");
                return;
            }
            
            _state = ServiceState.Running;
            Initialize(serviceConfig);

            LogState("Start");
        }
            

        /// <summary>
        /// Restart the OpenVPN Service.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public override void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            LogState("ReStart");
            SetConfig(serviceConfig);
            _state = ServiceState.Restarting;
            Stop();
            Start();
        }


        /// <summary>
        /// Effectively similar to restart in this context.
        /// </summary>
        /// <param name="serviceConfig"></param>
        public override void Reload(IEnumerable<IServiceConfig> serviceConfig = null) =>
            ReStart(serviceConfig);

        /// <summary>
        /// Stop the OpenVPN Service.
        /// Temporary configurations will also be deleted, and the tracking object will be emptied.
        /// </summary>
        public override void Stop()
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
            
            // Cleanup all of our port allocations so they may be re-allocated as needed.
            _portRegistry.CleanUp(this);
            
            LogState("Stop");
        }

        /// <summary>
        /// Get the state of the service.
        /// </summary>
        /// <returns></returns>
        public override ServiceState GetState() => _state;

        /// <summary>
        /// Get the list of configurations.
        /// The _vpnConfig is a private class variable, and this should be considered as a getter.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IServiceConfig> GetConfig() =>
            _vpnConfig;

        /// <summary>
        /// TODO: IMPLEMENT THIS FUNCTION.
        /// Get the state of the logger.
        /// </summary>
        /// <param name="caller"></param>
        public override void LogState(string caller) =>
            _logger.LogDebug($"OpenVPN ({caller}): current state is {_state}");

        /// <summary>
        /// Apply a list of configurations to this instance of the OpenVPN Class.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="restartNeeded"></param>
        public override void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            if (config == null) return;
            
            _vpnConfig = config as List<OpenVPNConfig>;
            if (restartNeeded)
                ReStart();

        }
            
    }
}