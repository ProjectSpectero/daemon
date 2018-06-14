using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Medallion.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Agreement.Kdf;
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
            // Convert the options into a new command holder.
            CommandHolder commandHolder = null;

            // Check the state of the service.
            if (caller.GetState() != ServiceState.Running && caller.GetState() != ServiceState.Restarting)
            {
                _logger.LogInformation("The service state prohibited a proccess from running.");
                return null;
            }
            else
            {
                // Check if we should run as root/admin.
                if (!processOptions.InvokeAsSuperuser)
                {
                    // Nope.
                    commandHolder = new CommandHolder
                    {
                        Options = processOptions,
                        Caller = caller,
                        Command = new Shell(
                            e => e.ThrowOnError()
                        ).Run(processOptions.Executable, processOptions.Arguments,
                            options: o => o
                                .DisposeOnExit(processOptions.DisposeOnExit)
                                .WorkingDirectory(processOptions.WorkingDirectory)
                        )
                    };
                }
                else
                {
                    // Yes, check the operating system to know what we have to do.
                    if (AppConfig.isWindows)
                    {
                        // runas verb, build the command holder witht he runas verb.
                        commandHolder = new CommandHolder
                        {
                            Options = processOptions,
                            Caller = caller,
                            Command = new Shell(
                                e => e.ThrowOnError()
                            ).Run(processOptions.Executable, processOptions.Arguments,
                                options: o => o
                                    .StartInfo(s => s
                                            // The runas attribute will run as administrator.
                                            .Verb = "runas"
                                    )
                                    .DisposeOnExit(processOptions.DisposeOnExit)
                                    .WorkingDirectory(processOptions.WorkingDirectory)
                            )
                        };
                    }
                    else if (AppConfig.isUnix)
                    {
                        // TODO: Test that we can use verb eventually, i'd rather have an explicit call right now though for sanity.
                        // sudo, a little more complex - copy all the objects into a new argument array.
                        string[] fixedArguments = {processOptions.Executable};
                        Array.Copy(processOptions.Arguments, 0, fixedArguments, fixedArguments.Length, processOptions.Arguments.Length);

                        // Build the command holder with a sudo as the executable.
                        commandHolder = new CommandHolder
                        {
                            Options = processOptions,
                            Caller = caller,
                            Command = new Shell(
                                e => e.ThrowOnError()
                            ).Run("sudo", fixedArguments,
                                options: o => o
                                    .DisposeOnExit(processOptions.DisposeOnExit)
                                    .WorkingDirectory(processOptions.WorkingDirectory)
                            )
                        };
                    }
                }

                // Attach command objects
                commandHolder.Options.streamProcessor.StandardOutputProcessor = CommandLogger.StandardAction();
                commandHolder.Options.streamProcessor.ErrorOutputProcessor = CommandLogger.ErrorAction();

                // Log to the console
                GetStreamProcessor(commandHolder).StandardOutputProcessor(_logger, commandHolder);
                GetStreamProcessor(commandHolder).ErrorOutputProcessor(_logger, commandHolder);

                // Check if we should monitor.
                if (commandHolder.Options.Monitor)
                    new Thread(() => Monitor(commandHolder, caller)).Start();

                // Keep track of the object.
                Track(commandHolder);

                // Return
                return commandHolder;
            }
        }

        public void Monitor(CommandHolder commandHolder, IService service)
        {
            // Wait for it to be tracked.
            while (!_runningCommands.Contains(commandHolder)) Thread.Sleep(100);

            // While we should track the process.
            while (_runningCommands.Contains(commandHolder))
            {
                // Check if we need to restart the command
                if (commandHolder.Options.DisposeOnExit)
                {
                    if (service.GetState() == ServiceState.Running)
                    {
                        if (commandHolder.Command.Process.HasExited)
                        {
                            _logger.LogWarning(string.Format("A process has exited gracefully."));
                            _runningCommands.Remove(commandHolder);
                        }
                    }
                    else
                    {
                        if (!commandHolder.Command.Process.HasExited)
                        {
                            _logger.LogWarning(
                                string.Format(
                                    "The service state has changed, Process ID {0} will be killed.",
                                    commandHolder.Command.Process.Id
                                )
                            );
                            commandHolder.Command.Process.Close();
                            _runningCommands.Remove(commandHolder);
                        }
                    }
                }
                else // if process not dispose on exit
                {
                    if (service.GetState() == ServiceState.Running)
                    {
                        if (commandHolder.Command.Process.HasExited)
                        {
                            _logger.LogWarning(
                                string.Format("A process has exited unexpectedly, and will be restarted.")
                            );
                            commandHolder.Command.Process.Start();
                        }
                    }
                    else
                    {
                        if (!commandHolder.Command.Process.HasExited)
                        {
                            _logger.LogWarning(
                                string.Format(
                                    "The service state has changed, Process ID {0} will be killed.",
                                    commandHolder.Command.Process.Id
                                )
                            );
                            commandHolder.Command.Process.Close();
                        }
                    }
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
        /// <param name="commandHolderList"></param>
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

        /// <summary>
        /// Gets the stream processor for the command holder
        /// </summary>
        /// <param name="commandHolder"></param>
        /// <returns></returns>
        public StreamProcessor GetStreamProcessor(CommandHolder commandHolder) => commandHolder.Options.streamProcessor;

        /// <summary>
        /// Reassign the command holder's command with the provided.
        /// </summary>
        /// <param name="commandHolder"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public CommandHolder ReassignCommand(CommandHolder commandHolder, Command command)
        {
            commandHolder.Command = command;
            return commandHolder;
        }
    }
}