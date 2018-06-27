using System.Diagnostics;
using Medallion.Shell;
using Spectero.daemon.Libraries.Services;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class CommandHolder
    {
        public Command Command { get; set; }
        public ProcessOptions Options { get; set; }
        public IService Caller { get; set; }
    }
}