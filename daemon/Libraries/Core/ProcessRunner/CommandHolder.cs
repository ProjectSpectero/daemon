using ServiceStack;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class CommandHolder
    {
        private Command Command { get; set; }
        private ProcessOptions Options { get; set; }
        private IService Caller { get; set; }
    }
}