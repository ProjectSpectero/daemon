using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public interface IProcessRunner
    {
        // The first one contains all details about how to run and configure the 3rd party binary, the 2nd one is for monitoring/such synchronization only.
        CommandHolder Run(ProcessOptions processOptions, IService caller);

        void CloseAllTrackedCommands();
        void CloseAllBelongingToService(IService service, bool force = false);
        void TerminateAllTrackedCommands();
        void RestartAllTrackedCommands(bool force);
    }
}