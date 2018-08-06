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
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Firewall.Rule;
using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Core.Firewall.Environments
{
    public class IPTables : IFirewallEnvironment
    {
        // Parent class
        private readonly Firewall _parent;

        private readonly IProcessRunner _processRunner;
        private readonly ILogger<object> _logger;

        // List of active firewall commands.
        private List<NetworkRule> _rules;

        /// <summary>
        /// Initialize the logger from the firewall handler.
        /// </summary>
        /// <param name="parent"></param>
        public IPTables(Firewall parent)
        {
            // Store the reference to the parrent
            _parent = parent;

            _processRunner = _parent.GetProcessRunner();
            _logger = _parent.GetLogger();

            // Initialize the rule list
            _rules = new List<NetworkRule>();
        }

        /// <summary>
        /// Add a rule to track.
        /// </summary>
        /// <param name="networkRule"></param>
        /// <exception cref="Exception"></exception>
        public void AddRule(NetworkRule networkRule)
        {
            // Build standard process options.
            var commandOptions = NetworkBuilder.BuildProcessOptions("iptables");

            // Get local interface information
            var interfaceInformation = GetDefaultInterface();

            // Determine how to handle the rule.
            switch (networkRule.Type)
            {
                // MASQUERADE
                case NetworkRuleType.Masquerade:
                    // Assign the argument.
                    commandOptions.Arguments = (
                            "-A " + NetworkBuilder.BuildTemplate(
                                NetworkRuleTemplates.MASQUERADE,
                                networkRule,
                                interfaceInformation
                            )
                        )
                        // Convert to string array.
                        .Split(" ");

                    // Tell the console
                    _logger.LogInformation("Created MASQUERADE rule for {0}", networkRule.Network);
                    break;

                // SNAT
                case NetworkRuleType.SourceNetworkAddressTranslation:
                    // Assign the argument
                    commandOptions.Arguments = (
                            "-A " + NetworkBuilder.BuildTemplate(
                                NetworkRuleTemplates.SNAT,
                                networkRule,
                                interfaceInformation
                            )
                        )
                        // Convert to string array.
                        .Split(" ");

                    // Tell the console.
                    _logger.LogInformation("Created SNAT rule for {0}", networkRule.Network);
                    break;

                // Unhandled Exception
                default:
                    _logger.LogError("Firewall environment was provided undefined rule type.");
                    throw FirewallExceptions.UnhandledNetworkRuleException();
            }

            // Run the process.
            _processRunner.Run(commandOptions);

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
            var processOptions = NetworkBuilder.BuildProcessOptions("iptables");

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
                    _logger.LogError("Firewall environment was provided undefined rule type.");
                    throw FirewallExceptions.UnhandledNetworkRuleException();
            }

            //TODO: Ask paul for help here. Not sure what we should do.
            _processRunner.Run(processOptions, null);

            // Forget the rule.
            _rules.Remove(networkRule);
        }

        /// <summary>
        /// Enable a MASQ Rule.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public NetworkRule Masquerade(string network, string networkInterface)
        {
            // Define the rule
            var rule = new NetworkRule()
            {
                Network = network,
                Interface = networkInterface
            };

            if (!AppConfig.IsOpenVZContainer())
                rule.Type = NetworkRuleType.Masquerade;
            else
            {
                _logger.LogWarning("OpenVZ Detected - Favoring SNAT rule over MASQUERADE.");
                rule.Type = NetworkRuleType.SourceNetworkAddressTranslation;
            }

            // Add the rule safely.
            AddRule(rule);

            // Return the rule if the user wants it.
            return rule;
        }

        /// <summary>
        /// Disable a MASQ rule.
        /// </summary>
        /// <param name="networkRule"></param>
        /// <exception cref="Exception"></exception>
        public void DisableMasquerade(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.Masquerade)
                throw FirewallExceptions.NetworkRuleMismatchException();

            // Delete the rule
            DeleteRule(networkRule);
        }

        /// <summary>
        /// Enable a SNAT rule.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="networkInterface"></param>
        /// <returns></returns>
        public NetworkRule SourceNetworkAddressTranslation(string network, string networkInterface)
        {
            // Define the rule
            var rule = new NetworkRule()
            {
                Type = NetworkRuleType.Masquerade,
                Network = network,
                Interface = networkInterface,
                Protocol = NetworkRuleProtocol.Tcp
            };

            // Add the rule safely.
            AddRule(rule);

            // Return the rule if the user wants it.
            return rule;
        }

        /// <summary>
        /// Disable a SNAT Rule.
        /// </summary>
        /// <param name="networkRule"></param>
        /// <exception cref="Exception"></exception>
        public void DisableSourceNetworkAddressTranslation(NetworkRule networkRule)
        {
            // Check if we have the right rule.
            if (networkRule.Type != NetworkRuleType.SourceNetworkAddressTranslation)
                throw FirewallExceptions.NetworkRuleMismatchException();

            // Safely delete the rule.
            DeleteRule(networkRule);
        }

        /// <summary>
        /// Gets the absolute path to the IP command.
        /// </summary>
        /// <returns></returns>
        private string GetIPCommandPath()
        {
            // Build layout of what we want to do.
            var ipProcessOptions = new ProcessOptions()
            {
                InvokeAsSuperuser = true,
                Monitor = false,
                DisposeOnExit = true,
                Executable = "which",
                Arguments = new[] {"ip"},
                ThrowOnError = true
            };

            // Execute the options
            var commandHolder = _processRunner.Run(ipProcessOptions);

            // Wait for exit.
            commandHolder.Command.Wait();

            // Return the data wae need.
            return commandHolder.Command.StandardOutput.ReadToEnd().Trim();
        }

        /// <summary>
        /// Retuns a new InterfaceInformation object with populated infomration about the system.
        /// </summary>
        /// <returns></returns>
        public InterfaceInformation GetDefaultInterface()
        {
            // Build layout of what we want to do.
            var ipProcessOptions = new ProcessOptions()
            {
                InvokeAsSuperuser = false,
                Monitor = false,
                DisposeOnExit = true,
                Executable = GetIPCommandPath(),
                Arguments = new[] {"route", "get", "8.8.8.8"},
                ThrowOnError = true
            };

            // Execute the options
            var commandHolder = _processRunner.Run(ipProcessOptions);

            // Wait for exit.
            commandHolder.Command.Wait();

            // Split the data
            var splitShellResponse = commandHolder.Command.StandardOutput.ReadToEnd().Split(" ");

            // Determine the source address.
            var sourceAddress = "0.0.0.0";
            for (var i = 0; i < splitShellResponse.Length; i++)
            {
                if (splitShellResponse[i] != "src") continue;
                sourceAddress = splitShellResponse[i + 1];
                break;
            }


            // Return the new format.
            return new InterfaceInformation()
            {
                Name = splitShellResponse[4],
                Address = sourceAddress
            };
        }

        /// <summary>
        /// Simple getter function to return the list of rules.
        /// </summary>
        /// <returns></returns>
        public List<NetworkRule> GetNetworkRules() => _rules;
    }
}