using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Firewall.Environments;

namespace Spectero.daemon.Libraries.Core.Firewall
{
    public class Firewall
    {
        /// <summary>
        /// Instance Holder
        /// This variable holds the reference to the process runner.
        /// </summary>
        private ProcessRunner.ProcessRunner _processRunner;

        /// <summary>
        /// Reference to the new logger for the specific firewall class.
        /// </summary>
        private ILogger<Firewall> _logger;

        /// <summary>
        /// Environment Holder
        /// This variable hosts the instance of the firewall for the specific operating system.
        /// </summary>
        private readonly IFirewallEnvironment _firewall;

        /// <summary>
        /// Class Constructor.
        /// </summary>
        /// <exception cref="???"></exception>
        public Firewall(ILogger<Firewall> logger, ProcessRunner.ProcessRunner processRunner)
        {
            // Store the reference to the process runner.
            _processRunner = processRunner;

            // Store the reference to the logger.
            _logger = logger;

            // Check if we're the following operating systems and create a new instance. 
            if (AppConfig.isWindows)
            {
                // TODO: Implement support for Windows Firewall.
                // We're not yet sure that we can do masquerading on windows.
                throw FirewallExceptions.UnsupportedOperatingSystemException();
            }
            else if (AppConfig.isLinux)
            {
                _firewall = new IPTables(this);
            }
            else if (AppConfig.isMac)
            {
                // TODO: Implement support for PF, OS X's equivelant of iptables.
                throw FirewallExceptions.UnsupportedOperatingSystemException();
            }
            else
            {
                throw FirewallExceptions.UnsupportedOperatingSystemException();
            }
        }

        /// <summary>
        /// Passthrough object, will allow the caller to use the class as such:
        /// Firewall.Rules.Masquerade();
        /// </summary>
        public IFirewallEnvironment Rules => _firewall;

        /// <summary>
        /// Pointer method to get the interface from the initialized firewall.
        /// </summary>
        /// <returns></returns>
        public InterfaceInformation GetInterface() => _firewall.GetDefaultInterface();

        /// <summary>
        /// Get the logger stored in the class.
        /// This function is meant to be called from the specific environment.
        /// </summary>
        /// <returns></returns>
        public ILogger<object> GetLogger() => _logger;

        /// <summary>
        /// Get the process runner stored in the class.
        /// This function is meant to be called from the specific environment.
        /// </summary>
        /// <returns></returns>
        public ProcessRunner.ProcessRunner GetProcessRunner() => _processRunner;
    }
}