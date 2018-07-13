using NClap.Metadata;
using NClap.Repl;

namespace Spectero.daemon.CLI.Commands
{
    public class Shell : BaseJob
    {
        private readonly Loop _loop = new Loop(typeof(Commands));


        public override CommandResult Execute()
        {
            _loop.Execute();
            return CommandResult.Success;
        }
        
        public override bool IsDataCommand() => true;
    }
}