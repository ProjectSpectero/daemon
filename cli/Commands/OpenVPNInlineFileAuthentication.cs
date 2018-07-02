using System.Collections.Generic;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class OpenVPNInlineFileAuthentication : BaseJob
    {
        [PositionalArgument(
            ArgumentFlags.Required, Position = 0,
            Description = "The service scope being requested from the system for the user. Usually one of { HTTPProxy | OpenVPN | SSHTunnel | ShadowSOCKS }"
        )]
        private string Scope { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The name of the file with authentication data.")]
        private string Filename { get; set; }

        public override CommandResult Execute()
        {
            var request = new AuthenticationRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"filename", Filename},
                {"serviceScope", Scope}
            };

            return HandleRequest(null, request, body);
        }
    }
}