using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Medallion.Shell;
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
        private List<CommandHolder> _runningCommands;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="configMonitor"></param>
        /// <param name="logger"></param>
        public ProcessRunner(IOptionsMonitor<AppConfig> configMonitor, ILogger<ProcessRunner> logger)
        {
            _config = configMonitor.CurrentValue;
            _logger = logger;

            // This tracks the long running processes, what options triggered the process, and the caller (whose state we have to be synced with)
            _runningCommands = new List<CommandHolder>();
        }

        /// <summary>
        /// Start a new command holder based on the provided process options and caller.
        /// </summary>
        /// <param name="processOptions"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public CommandHolder Run(ProcessOptions processOptions, IService caller)
        {
            // Convert the options into a new command holder.
            var commandHolder = new CommandHolder
            {
                Options = processOptions,
                Caller = caller
            };

            // Keep track of the object.
            Track(commandHolder);

            // Return
            return commandHolder;
        }

        /// <summary>
        /// Start tracking a command.
        /// </summary>
        /// <param name="referencedCommand"></param>
        public void Track(CommandHolder referencedCommand) => _runningCommands.Add(referencedCommand);

        /// <summary>
        /// Start tracking a list of processes
        /// </summary>
        /// <param name="referencedCommandList"></param>
        public void Track(List<CommandHolder> referencedCommandList)
        {
            foreach (var command in referencedCommandList)
                _runningCommands.Add(command);
        }

        /// <summary>
        /// Stop tracking a process.
        /// </summary>
        /// <param name="referencedCommandHolder"></param>
        /// <returns></returns>
        public bool Untrack(CommandHolder referencedCommandHolder)
        {
            // Check to see if the process is already being tracked.
            if (_runningCommands.Contains(referencedCommandHolder))
            {
                // The process is being tracked, remove it.
                _runningCommands.Remove(referencedCommandHolder);
                return true;
            }
            else
            {
                // The process is not being tracked.
                return false;
            }
        }

        /// <summary>
        /// Get a list of the running command holders.
        /// </summary>
        /// <returns></returns>
        public List<CommandHolder> GetRunningCommands() => _runningCommands;

        /// <summary>
        /// Get the IService caller object of the provided CommandHolder.
        /// </summary>
        /// <param name="referencedCommandHolder"></param>
        /// <returns></returns>
        public IService GetCommandCallerService(CommandHolder referencedCommandHolder) =>
            referencedCommandHolder.Caller;

        /// <summary>
        /// Forget all of the tracked processes.
        /// The processes that were previously tracked will still retain their state, just forgotten.
        /// </summary>
        public void ClearTrackedProcesses()
        {
            _runningCommands = new List<CommandHolder>();
        }

        /// <summary>
        /// close all tracked processes gracefully.
        /// </summary>
        public void CloseAllTrackedCommands()
        {
            foreach (var commandHolder in _runningCommands)
                commandHolder.Command.Process.Close();
        }

        /// <summary>
        /// Terminate all tracked processes forcefully.
        /// </summary>
        public void TerminateAllTrackedCommands()
        {
            foreach (var commandHolder in _runningCommands)
                commandHolder.Command.Process.Kill();
        }

        /// <summary>
        /// Start all the commands in the class list.
        /// This function is meant to only be called internally.
        /// </summary>
        private void StartAllTrackedCommands()
        {
            foreach (var commandHolder in _runningCommands)
                commandHolder.Command.Process.Start();
        }

        /// <summary>
        /// Restart all tracked commands.
        /// </summary>
        public void RestartAllTrackedProcesses(bool force = false)
        {
            // Check if we should agressively close all the processes.
            if (!force)
                // Safely.
                CloseAllTrackedCommands();
            else
                // Aggressive.
                TerminateAllTrackedCommands();

            // Start them all again.
            StartAllTrackedCommands();
        }
    }
}