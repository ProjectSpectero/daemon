using System;
using System.Collections.Generic;
using System.IO;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class InlineFileAuth : BaseJob
    {
        [PositionalArgument(
            ArgumentFlags.Required,
            Position = 0,
            Description = "The service scope being requested from the system for the user. Usually one of { HTTPProxy | OpenVPN | SSHTunnel | ShadowSOCKS }"
        )]
        private string Scope { get; set; }

        [PositionalArgument(
            ArgumentFlags.Required,
            Position = 1,
            Description = "The name of the file with authentication data."
        )]
        private string Filename { get; set; }

        public override CommandResult Execute()
        {
            string[] configFileObject;
            string pluckedUsername;
            string pluckedPassword;

            // Attempt to read the config.
            try
            {
                // Read.
                configFileObject = File.ReadAllLines(Filename);

                // Pluck the data in order
                pluckedUsername = configFileObject[0];
                pluckedPassword = configFileObject[1];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return CommandResult.RuntimeFailure;
            }

            // Build the request
            var request = new AuthenticationRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"authKey", pluckedUsername},
                {"password", pluckedPassword},
                {"serviceScope", Scope}
            };

            // Run it.
            return HandleRequest(null, request, body);
        }
    }
}