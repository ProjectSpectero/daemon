using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Spectero.daemon.Libraries.Core.Firewall.Rule;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class WindowsFirewall : IFirewallEnvironment
    {
        private Firewall _parent;

        public WindowsFirewall(Firewall parent)
        {
            _parent = parent;
        }

        public NetworkRule Masquerade(string network, string networkInterface)
        {
            throw new NotImplementedException();
        }

        public void DisableMasquerade(NetworkRule networkRule)
        {
            throw new NotImplementedException();
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

        public List<NetworkRule> GetNetworkRules()
        {
            throw new NotImplementedException();
        }
    }
}