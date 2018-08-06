using Spectero.daemon.CLI.Requests;
using CommandResult = NClap.Metadata.CommandResult;

namespace Spectero.daemon.CLI.Commands
{
    class Shutdown : BaseJob
    {
        public override CommandResult Execute()
        {
            var request = new ShutdownRequest(ServiceProvider);
            return HandleRequest(null, request, caller: this);
        }

        public override bool IsDataCommand() => false;
    }
}