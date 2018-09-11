/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        [JsonIgnore]
        public StreamProcessor streamProcessor = new StreamProcessor();

        // The working directory of where the OpenVPN Configuration should have its diffie hellman keys.
        public string WorkingDirectory;

        // Should medallion shell throw an error when there's a problem.
        public bool ThrowOnError = true;

        // Should we attach logging instances to it.
        public bool AttachLogToConsole = false;
    }
}