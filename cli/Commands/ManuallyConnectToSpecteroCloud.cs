using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.CLI.Requests;
using System;
using System.Collections.Generic;

namespace Spectero.daemon.CLI.Commands
{
    public class ManuallyConnectToSpecteroCloud : BaseJob
    {

        [NamedArgument(ArgumentFlags.Required, ShortName = "id", Description = "The node's Id")]
        public string NodeId { get; set; }

        [NamedArgument(ArgumentFlags.Required, ShortName = "key", Description = "The node's key")]
        public string NodeKey { get; set; }

        [NamedArgument(ArgumentFlags.Optional, ShortName = "force", Description = "Force connect")]
        public bool ForceConnect = false;

        public override CommandResult Execute()
        {
            var request = new ManualConnectToCloudRequest(ServiceProvider);
            var response = request.Perform(new Dictionary<string, object>
            {
                {"force", ForceConnect },
                {"nodeId", NodeId },
                {"nodeKey", NodeKey }
            });

            string json = JsonConvert.SerializeObject(response);

            string jsonFormatted = JValue.Parse(json).ToString(Formatting.Indented);

            Console.WriteLine(jsonFormatted);

            return CommandResult.Success;
        }
    }
}