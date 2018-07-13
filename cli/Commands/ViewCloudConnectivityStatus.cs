using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class ViewCloudConnectivityStatus : BaseJob
    {
        public override CommandResult Execute()
        {
            var request = new GetCloudConnectivityStatusRequest(ServiceProvider);
            return HandleRequest(null, request);
        }
        
        public override bool IsDataCommand() => true;
    }
}