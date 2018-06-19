using System.Collections.Generic;
using Spectero.daemon.Libraries.Core.Firewall.Rule;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class MacOSPortFilter : IFirewallEnvironment
    {
        public NetworkRule Masquerade(string network, string networkInterface)
        {
            throw new System.NotImplementedException();
        }

        public void DisableMasquerade(NetworkRule networkRule)
        {
            throw new System.NotImplementedException();
        }

        public NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface)
        {
            throw new System.NotImplementedException();
        }

        public void DisableSourceNetworkAddressTranslation(NetworkRule networkRule)
        {
            throw new System.NotImplementedException();
        }

        public void AddRule(NetworkRule networkRule)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteRule(NetworkRule networkRule)
        {
            throw new System.NotImplementedException();
        }

        public InterfaceInformation GetDefaultInterface()
        {
            throw new System.NotImplementedException();
        }

        public List<NetworkRule> GetNetworkRules()
        {
            throw new System.NotImplementedException();
        }
    }
}