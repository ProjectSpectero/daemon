using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class ConnectToSpecteroCloud : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "User's Node Key")]
        private string NodeKey { get; set; }

        public override CommandResult Execute()
        {
            // TODO: @alex, invoke the right request, get the response, parse it and show the user the right output.

            var request = new ConnectToCloudRequest(ServiceProvider);
            var response = request.Perform(NodeKey); // TODO: parse this and tell the user what's up

            return CommandResult.Success;
        }
    }
}