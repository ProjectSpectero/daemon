using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        private List<CommandHolder> _runningCommands = new List<CommandHolder>();

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="configMonitor"></param>
        /// <param name="logger"></param>
        public ProcessRunner(IOptionsMonitor<AppConfig> configMonitor, ILogger<ProcessRunner> logger)
        {
            // Inherit the attributes.
            _config = configMonitor.CurrentValue;
            _logger = logger;
        }

        /// <summary>
        /// Start a new command holder based on the provided process options and caller.
        /// </summary>
        /// <param name="processOptions"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public CommandHolder Run(ProcessOptions processOptions, IService caller)
        {
            if (caller.GetState() != ServiceState.Running || caller.GetState() != ServiceState.Restarting)
                throw new Exception("The service state prohibits a proccess from running.");

            // Other process related information.
            var processInfo = new ProcessStartInfo()
            {
                WorkingDirectory = processOptions.WorkingDirectory
            };

            // Convert the options into a new command holder.
            var commandHolder = new CommandHolder
            {
                Options = processOptions,
                Caller = caller,
                Command = new Shell(
                    e => e.ThrowOnError()
                ).Run(processOptions.Executable, processOptions.Arguments, processInfo)
            };

            // Monitor the output.
            CommandLogger.LatchQuickly(_logger, commandHolder);

            // Check if we should monitor.
            if (commandHolder.Options.Monitor)
                new Thread(() => Monitor(commandHolder, caller)).Start();

            // Keep track of the object.
            Track(commandHolder);

            // Return
            return commandHolder;
        }

        public void Monitor(CommandHolder commandHolder, IService service)
        {
            // Wait for it to be tracked.
            while (!_runningCommands.Contains(commandHolder)) Thread.Sleep(100);

            // While we should track the process.
            while (_runningCommands.Contains(commandHolder))
            {
                if (service.GetState() == ServiceState.Running)
                {
                    // Check if we need to restart the command
                    if (commandHolder.Command.Process.HasExited)
                    {
                        if (commandHolder.Options.DisposeOnExit)
                        {
                            _runningCommands.Remove(commandHolder);
                            _logger.LogInformation("A process has been disposed of gracefully.");
                        }
                        else
                        {
                            // Check if we should restar the process if it does.
                            if (commandHolder.Options.Daemonized)
                            {
                                if (service.GetState() == ServiceState.Running)
                                {
                                    // Restart the process.
                                    commandHolder.Command.Process.Start();
                                    _logger.LogWarning("The process has died, and was instructed to restart.");
                                }
                                else
                                {
                                    _logger.LogError("Failed to restart the process as the state of the service was not running.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Check to see if the process is still running, if so close it.
                    if (!commandHolder.Command.Process.HasExited)
                        _logger.LogWarning("The service state has changed, the process has been instructed to close.");
                    commandHolder.Command.Process.Close();

                    // Remove from the list
                    _runningCommands.Remove(commandHolder);
                }

                // Wait for the monitoring interval.
                Thread.Sleep(commandHolder.Options.MonitoringInterval * 1000);
            }
        }

        /// <summary>
        /// Start tracking a command.
        /// </summary>
        /// <param name="commandHolder"></param>
        public void Track(CommandHolder commandHolder) => _runningCommands.Add(commandHolder);

        /// <summary>
        /// Start tracking a list of processes
        /// </summary>
        /// <param name="referencedCommandList"></param>
        public void Track(List<CommandHolder> commandHolderList)
        {
            foreach (var command in commandHolderList)
                _runningCommands.Add(command);
        }

        /// <summary>
        /// Stop tracking a process.
        /// </summary>
        /// <param name="commandHolder"></param>
        /// <returns></returns>
        public bool Untrack(CommandHolder commandHolder)
        {
            // Check to see if the process is already being tracked.
            if (_runningCommands.Contains(commandHolder))
            {
                // The process is being tracked, remove it.
                _runningCommands.Remove(commandHolder);
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
        public void RestartAllTrackedCommands(bool force = false)
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