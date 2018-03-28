using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.CLI.Requests;
using System;

namespace Spectero.daemon.CLI.Commands
{
    public class ViewCloudConnectivityStatus : BaseJob
    {
        public override CommandResult Execute()
        {
            var request = new GetCloudConnectivityStatusRequest(ServiceProvider);

            var response = request.Perform();

            DisplayResult(response);

            return CommandResult.Success;
        }
    }
}