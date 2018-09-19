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
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Core.Firewall.Rule;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class WindowsFirewall : IFirewallEnvironment
    {
        private readonly Firewall _parent;
        private readonly ILogger<object> _logger;
        private readonly List<NetworkRule> _rules;

        public WindowsFirewall(Firewall parent)
        {
            _parent = parent;
            _logger = _parent.GetLogger();
            _rules = new List<NetworkRule>();
        }

        public NetworkRule Masquerade(string network, string networkInterface)
        {
            _logger.LogWarning($"Masquerade rule requested for {network} on {networkInterface}, but there is NO NAT/MASQUERADE support on this platform (Windows) yet!");
            
            // TODO: actually make masquerade work on Windows.
            return new NetworkRule()
            {
                Interface = network,
                Network = network
            };
        }

        public void DisableMasquerade(NetworkRule networkRule)
        {
            _logger.LogWarning($"Removal of masquerade rule requested for {networkRule.Network} on {networkRule.Interface}, but there is NO NAT/MASQUERADE support on this platform (Windows) yet!");
        }

        public NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface)
        {
            throw new NotImplementedException();
        }

        public void DisableSourceNetworkAddressTranslation(NetworkRule networkRule)
        {
            throw new NotImplementedException();
        }

        public void AddRule(NetworkRule networkRule)
        {
            throw new NotImplementedException();
        }

        public void DeleteRule(NetworkRule networkRule)
        {
            throw new NotImplementedException();
        }

        public InterfaceInformation GetDefaultInterface()
        {
            var nic = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback && i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
            
            return new InterfaceInformation()
            {
                Name = nic.Name,
                Address = nic.GetPhysicalAddress().ToString()
            };
        }

        public List<NetworkRule> GetNetworkRules() => _rules;
    }
}