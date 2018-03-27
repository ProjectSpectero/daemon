using NClap.Metadata;
using Spectero.daemon.CLI.Requests;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Spectero.daemon.CLI.Commands
{
    public class ConnectToSpecteroCloud : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "User's Node Key")]
        private string NodeKey { get; set; }

        public override CommandResult Execute()
        {
            var request = new ConnectToCloudRequest(ServiceProvider);
            var response = request.Perform(NodeKey);

            string json = JsonConvert.SerializeObject(response);

            string jsonFormatted = JValue.Parse(json).ToString(Formatting.Indented);

            Console.WriteLine(jsonFormatted);

            return CommandResult.Success;
        }
    }
}
