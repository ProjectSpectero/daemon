using System;
using Microsoft.Extensions.Logging;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class StreamProcessor
    {
        public Action<ILogger<ProcessRunner>, CommandHolder> StandardOutputProcessor;
        public Action<ILogger<ProcessRunner>, CommandHolder> ErrorOutputProcessor;
    }

    public class ProcessOptions
    {
        // Windows: Run as administrator.
        // Linux: Run as root.
        public bool InvokeAsSuperuser = false;

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
        public StreamProcessor streamProcessor = new StreamProcessor();

        // The working directory of where the OpenVPN Configuration should have its diffie hellman keys.
        public string WorkingDirectory;

        // Should medallion shell throw an error when there's a problem.
        public bool ThrowOnError = true;
    }
}