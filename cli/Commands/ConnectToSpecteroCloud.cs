using NClap.Metadata;
using RestSharp;
using Spectero.daemon.CLI.Requests;
using System;

namespace Spectero.daemon.CLI.Commands
{
    public class ConnectToSpecteroCloud : SynchronousCommand
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "Node key")]
        public string NodeKey { get; set; }

        private readonly IRestClient client;

        public ConnectToSpecteroCloud(IRestClient client)
        {
            this.client = client;
        }

        public override CommandResult Execute()
        {
            // TODO: @alex, invoke the right request, get the response, parse it and show the user the right output.
            var request = new ConnectToCloudRequest(client);
            var response = request.Perform(NodeKey);

            Console.WriteLine(response);
            return CommandResult.Success;
        }
    }
}