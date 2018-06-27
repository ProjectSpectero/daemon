using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Medallion.Shell;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Firewall.Rule;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class IPTables : IFirewallEnvironment
    {
        // Parent class
        private Firewall _firewallHandler;

        // List of active firewall commands.
        private List<NetworkRule> _rules;

        /// <summary>
        /// Initialize the logger from the firewall handler.
        /// </summary>
        /// <param name="parent"></param>
        public IPTables(Firewall parent)
        {
            // Store the reference to the parrent
            _firewallHandler = parent;
        }

        /// <summary>
        /// Add a rule to track.
        /// </summary>
        /// <param name="networkRule"></param>
        /// <exception cref="Exception"></exception>
        public void AddRule(NetworkRule networkRule)
        {
            // Build standard process options.
            var processOptions = NetworkBuilder.BuildProcessOptions("iptables", true);

            // Get local interface information
            var interfaceInformation = GetDefaultInterface();

            // Determine how to handle the rule.
            switch (networkRule.Type)
            {
                // MASQUERADE
                case NetworkRuleType.Masquerade:
                    processOptions.Arguments = ("-A " + NetworkBuilder.BuildTemplate(NetworkRuleTemplates.MASQUERADE,
                                                    networkRule, interfaceInformation)).Split(" ");
                    _firewallHandler.GetLogger().LogInformation("Created MASQUERADE rule for {0}", networkRule.Network);
                    break;

                // SNAT
                case NetworkRuleType.SourceNetworkAddressTranslation:
                    processOptions.Arguments =
                        ("-A " + NetworkBuilder.BuildTemplate(NetworkRuleTemplates.SNAT, networkRule,
                             interfaceInformation)).Split(" ");
                    _firewallHandler.GetLogger().LogInformation("Created SNAT rule for {0}", networkRule.Network);
                    break;

                // Unhandled Exception
                default:
                    _firewallHandler.GetLogger().LogError("Firewall environment was provided undefined rule type.");
                    throw FirewallExceptions.UnhandledNetworkRuleException();
            }

            //TODO: Ask paul for help here. Not sure what we should do.
            _firewallHandler.GetProcessRunner().RunSingle(processOptions);

            // Track the rule.
            _rules.Add(networkRule);
        }

        /// <summary>
        /// Delete a rule from the tracked objects.
        /// </summary>
        /// <param name="networkRule"></param>
        /// <exception cref="Exception"></exception>
        public void DeleteRule(NetworkRule networkRule)
        {
            // Build standard process options.
            var processOptions = NetworkBuilder.BuildProcessOptions("iptables", true);

            // Get local interface information
            var interfaceInformation = GetDefaultInterface();

            // Determine how to handle the rule.
            switch (networkRule.Type)
            {
                // MASQUERADE
                case NetworkRuleType.Masquerade:
                    processOptions.Arguments = ("-D " + NetworkBuilder.BuildTemplate(NetworkRuleTemplates.MASQUERADE, networkRule, interfaceInformation)).Split(" ");
                    break;

                // SNAT
                case NetworkRuleType.SourceNetworkAddressTranslation:
                    processOptions.Arguments =
                        ("-D " + NetworkBuilder.BuildTemplate(NetworkRuleTemplates.SNAT, networkRule, interfaceInformation)).Split(" ");
                    break;

                // Unhandled Exception
                default:
                    _firewallHandler.GetLogger().LogError("Firewall environment was provided undefined rule type.");
                    throw FirewallExceptions.UnhandledNetworkRuleException();
            }

            //TODO: Ask paul for help here. Not sure what we should do.
            _firewallHandler.GetProcessRunner().Run(processOptions, null);

            // Forget the rule.
            _rules.Remove(networkRule);
        }

        public NetworkRule Masquerade(string network, string networkInterface)
        {
            // Define the rule
            var rule = new NetworkRule()
            {
                Network = network,
                Interface = network
            };

            if (!AppConfig.IsOpenVZContainer())
                rule.Type = NetworkRuleType.Masquerade;
            else
            {
                _firewallHandler.GetLogger().LogWarning("OpenVZ Detected - Favoring SNAT rule over MASQUERADE.");
                rule.Type = NetworkRuleType.SourceNetworkAddressTranslation;
            }
            
            // Add the rule safely.
            AddRule(rule);

            // Return the rule if the user wants it.
            return rule;
        }

        public void DisableMasquerade(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.Masquerade)
                throw FirewallExceptions.NetworkRuleMismatchException();

            // Delete the rule
            DeleteRule(networkRule);
        }

        public NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface)
        {
            // Define the rule
            var rule = new NetworkRule()
            {
                Type = NetworkRuleType.Masquerade,
                Network = network,
                Interface = network,
                Protocol = NetworkRuleProtocol.Tcp
            };

            // Add the rule safely.
            AddRule(rule);

            // Return the rule if the user wants it.
            return rule;
        }

        public void DisableSourceNetworkAddressTranslation(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.SourceNetworkAddressTranslation)
                throw FirewallExceptions.NetworkRuleMismatchException();

            // Safely delete the rule.
            DeleteRule(networkRule);
        }

        public string GetIPCommandPath()
        {
            var cmd = Command.Run("which", "ip");
            cmd.Wait();
            return cmd.StandardOutput.ReadToEnd().Trim();
        }

        public InterfaceInformation GetDefaultInterface()
        {
            var shell = new Shell(o => o.ThrowIfNull());
            var cmd = shell.Run(GetIPCommandPath(), "route", "get", "8.8.8.8");
            cmd.Wait();
            var splitShellResponse = cmd.StandardOutput.ReadToEnd().Split(" ");

            return new InterfaceInformation()
            {
                Name = splitShellResponse[4],
                Address = splitShellResponse[6]
            };
        }

        /// <summary>
        /// Simple getter function to return the list of rules.
        /// </summary>
        /// <returns></returns>
        public List<NetworkRule> GetNetworkRules() => _rules;
    }
}