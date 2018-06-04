using System;
using Medallion.Shell;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class ProcessOptions
    {
        // The primary executable to call
        public string Executable;

        // The list of arguments to pass to the executable
        public string[] Arguments;

        // Is the process long running?
        public bool Daemonized = true;

        // Should we nanny the process and restart it if it dies by checking every `MonitoringInterval`?
        public bool Monitor = true;

        // This is in seconds
        public int MonitoringInterval = 30;

        // This is in seconds, by default there are none.
        public int ProcessTimeout;

        // Passthru param for Medallion
        public bool DisposeOnExit = false;

        // This is the function that we'll attach to the STDOUT/STDERR streams.
        // We'll also bundle a default implementation in the ProcessRunner if this is null which just `Log.Debug`s the generated data.
        public Action<Command> StreamProcessor;

        public string WorkingDirectory;
    }
}