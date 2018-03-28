using NClap.Metadata;
using Spectero.daemon.CLI.Requests;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Spectero.daemon.CLI.Commands
{
    public class ConnectToSpecteroCloud : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "User's Node Key")]
        private string NodeKey { get; set; }

        public override CommandResult Execute()
        {
            var request = new ConnectToCloudRequest(ServiceProvider);
            var response = request.Perform(new Dictionary<string, object>
            {
                {"nodeKey", NodeKey }
            });

            DisplayResult(response);

            return CommandResult.Success;
        }
    }
}
