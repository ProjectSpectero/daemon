using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Remotion.Linq.Parsing.Structure.IntermediateModel;
using ServiceStack;
using Spectero.daemon.Libraries.Core.Firewall.Rule;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class IPTables : IFirewallEnvironment
    {
        // Interface to the logger.
        private readonly ILogger<object> _logger;

        private List<NetworkRule> _rules;

        /// <summary>
        /// Initialize the logger from the firewall handler.
        /// </summary>
        /// <param name="logger"></param>
        public IPTables(ILogger<object> logger)
        {
            _logger = logger;
        }

        public NetworkRule Masquerade(string network, string networkInterface)
        {
            // Run the firewall command (-A for Append)
            Process.Start("iptables", string.Format("-A POSTROUTING -S {0} -o {1} -J MASQUERADE", network, networkInterface));

            // Define the rule
            var rule = new NetworkRule()
            {
                Type = NetworkRuleType.Masquerade,
                Network = network,
                Interface = network
            };

            // Keep track of what we did
            _rules.Add(rule);

            // Tell the console.
            _logger.LogDebug("Enabled masquerade rule on interface {1} for network {0}", network, networkInterface);

            // Return the rule if the user wants it.
            return rule;
        }

        public void DisableMasquerade(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.Masquerade) throw NetworkRuleMismatchException();

            // Run the firewall command (-D for Delete)
            Process.Start("iptables", string.Format("-D POSTROUTING -S {0} -o {1} -J MASQUERADE", networkRule.Network, networkRule.Interface));

            // Remove from the tracking.
            _rules.Remove(networkRule);

            // Tell the console.
            _logger.LogDebug("Disabled masquerade rule on interface {1} for network {0}", networkRule.Network, networkRule.Interface);
        }

        public NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface)
        {
            // Run the firewall command (-A for Append)
            Process.Start("iptables", string.Format("-t nat -A POSTROUTING -p TCP -o {1} -J SNAT --to {0}", network, networkInterface));

            var rule = new NetworkRule()
            {
                Type = NetworkRuleType.SourceNetworkAddressTranslation,
                Network = network,
                Interface = network,
                Protocol = NetworkRuleProtocol.Tcp
            };

            // Keep track of what we did
            _rules.Add(rule);

            // Log to the console.
            _logger.LogDebug("Enabled masquerade rule on interface {1} for network {0}", network, networkInterface);

            // Return the rule if the user wants it.
            return rule;
        }

        public void DisableSourceNetworkAddressTranslation(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.SourceNetworkAddressTranslation) throw NetworkRuleMismatchException();

            // Run the firewall command (-D for Delete)
            Process.Start("iptables", string.Format("-t nat -D POSTROUTING -p TCP -o {1} -J SNAT --to {0}", networkRule.Network, networkRule.Interface));

            // Remove from the tracking.
            _rules.Remove(networkRule);

            // Tell the console.
            _logger.LogDebug("Disabled masquerade rule on interface {1} for network {0}", networkRule.Network, networkRule.Interface);
        }

        public string GetDefaultInterface()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Generic exception to inform the console that the application has provided the wrong ruleset to the wrong function.
        /// </summary>
        /// <returns></returns>
        private Exception NetworkRuleMismatchException()
        {
            return new Exception("The NetworkRule object you provided to the function is incompatable.");
        }

        /// <summary>
        /// Simple getter function to return the list of rules.
        /// </summary>
        /// <returns></returns>
        public List<NetworkRule> GetNetworkRules() => _rules;
    }
}