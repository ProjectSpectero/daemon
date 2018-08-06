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
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Firewall.Environments;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Core.Firewall
{
    public class Firewall
    {
        /// <summary>
        /// Instance Holder
        /// This variable holds the reference to the process runner.
        /// </summary>
        private ProcessRunner.IProcessRunner _processRunner;

        /// <summary>
        /// Reference to the new logger for the specific firewall class.
        /// </summary>
        private ILogger<ServiceManager> _logger;

        /// <summary>
        /// Environment Holder
        /// This variable hosts the instance of the firewall for the specific operating system.
        /// </summary>
        private readonly IFirewallEnvironment _firewall;

        /// <summary>
        /// Class Constructor.
        /// </summary>
        /// <exception cref="???"></exception>
        public Firewall(ILogger<ServiceManager> logger, ProcessRunner.IProcessRunner processRunner)
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
                // throw FirewallExceptions.UnsupportedOperatingSystemException();

                _firewall = new WindowsFirewall(this);
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
                // throw FirewallExceptions.UnsupportedOperatingSystemException();
                _firewall = new MacOSPortFilter(this);
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
        public InterfaceInformation GetDefaultInterface() => _firewall.GetDefaultInterface();

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
        public ProcessRunner.IProcessRunner GetProcessRunner() => _processRunner;
    }
}