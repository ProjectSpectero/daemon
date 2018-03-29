using NClap.Metadata;
using Spectero.daemon.CLI.Requests;
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
            var body = new Dictionary<string, object>
            {
                {"nodeKey", NodeKey}
            };

            return HandleRequest(null, request, body);
        }
    }
}
