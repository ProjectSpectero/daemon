using System.Collections.Generic;
using Spectero.daemon.Libraries.Core.Firewall.Rule;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public interface IFirewallEnvironment
    {
        // Basic firewall functions
        NetworkRule Masquerade(string network, string networkInterface);
        void DisableMasquerade(NetworkRule networkRule);

        NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface);
        void DisableSourceNetworkAddressTranslation(NetworkRule networkRule);

        // Interface
        string GetDefaultInterface();

        // Tracked commands
        List<NetworkRule> GetNetworkRules();
    }
}