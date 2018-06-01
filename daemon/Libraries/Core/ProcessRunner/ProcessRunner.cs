using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class ProcessRunner : IProcessRunner
    {
        private readonly AppConfig _config;
        private readonly ILogger<ProcessRunner> _logger;
        private readonly List<CommandHolder> _runningCommands;

        public ProcessRunner(IOptionsMonitor<AppConfig> configMonitor, ILogger<ProcessRunner> logger)
        {
            _config = configMonitor.CurrentValue;
            _logger = logger;

            // This tracks the long running processes, what options triggered the process, and the caller (whose state we have to synced with)
            _runningCommands = new List<CommandHolder>();
        }

        public CommandHolder Run(ProcessOptions processOptions, IService caller)
        {
            // TODO: @Andrew - implement this
            // If process is Daemonized, add it to _runningCommands.

            // Process restart should ONLY be attempted if the caller itself is stull running (Run IService#GetState), and try to do these in a async way.
       
            throw new System.NotImplementedException();
        }
    }
}