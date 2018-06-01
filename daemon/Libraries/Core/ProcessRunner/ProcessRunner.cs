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
        private readonly ConcurrentDictionary<IService, List<CommandHolder>> _runningCommands;

        public ProcessRunner(IOptionsMonitor<AppConfig> configMonitor, ILogger<ProcessRunner> logger)
        {
            _config = configMonitor.CurrentValue;
            _logger = logger;

            // This is a mapping of a service -> list(spawned 3rd party processes)
            _runningCommands = new ConcurrentDictionary<IService, List<CommandHolder>>();
        }

        public CommandHolder Run(ProcessOptions processOptions, IService caller)
        {
            // TODO: @Andrew - implement this
            // If process is Daemonized, add it to _runningCommands.
       
            throw new System.NotImplementedException();
        }
    }
}