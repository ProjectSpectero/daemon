using Spectero.daemon.Libraries.Services;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public interface IProcessRunner
    {
        // The first one contains all details about how to run and configure the 3rd party binary, the 2nd one is for monitoring/such synchronization only.
        CommandHolder Run(ProcessOptions processOptions, IService caller);
    }
}