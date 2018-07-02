using System;
using System.Collections.Generic;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class ScopedAuthentication : BaseJob
    {
        // TODO: Figure out why NamedArguments didn't work here, NCAP bug possibly?
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The service scope being requested from the system for the user. Usually one of { HTTPProxy | OpenVPN | SSHTunnel | ShadowSOCKS }")]
        private string Scope { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "The username being attempted")]
        private string Username { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 2, Description = "The user's password")]
        private string Password { get; set; }


        public override CommandResult Execute()
        {
            var request = new AuthenticationRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"authKey", Username},
                {"password", Password },
                {"serviceScope", Scope }
            };

            return HandleRequest(null, request, body);
        }
    }
}
