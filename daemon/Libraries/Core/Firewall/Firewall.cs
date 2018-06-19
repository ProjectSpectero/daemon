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
        /// This variable hosts the instance of the firewall.
        /// </summary>
        private IFirewallEnvironment _firewall;

        /// <summary>
        /// Class Constructor.
        /// </summary>
        /// <exception cref="???"></exception>
        public Firewall(ILogger<object> logger)
        {
            // Check if we're the following operating systems and create a new instance. 
            if (AppConfig.isWindows)
            {
                // TODO: Implement support for Windows Firewall.
                // We're not yet sure that we can do masquerading on windows.
                throw FirewallExceptions.UnsupportedOperatingSystemException();
            }
            else if (AppConfig.isLinux)
            {
                this._firewall = new IPTables(logger);
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
        /// Firewall.Rule.Masquerade();
        /// </summary>
        public IFirewallEnvironment Rule => _firewall;

        /// <summary>
        /// Pointer method to get the interface from the initialized firewall.
        /// </summary>
        /// <returns></returns>
        public InterfaceInformation GetInterface() => _firewall.GetDefaultInterface();

        
    }
}