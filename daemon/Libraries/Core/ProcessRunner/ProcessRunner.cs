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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ProcessLogger _defaultProcessLogger;
        
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
            _defaultProcessLogger = new ProcessLogger(_logger);
        }

        /// <summary>
        /// Start a new command holder based on the provided process options and caller.
        /// </summary>
        /// <param name="processOptions"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        public CommandHolder Run(ProcessOptions processOptions, IProcessTrackable caller = null)
        {
            // Convert the options into a new command holder.
            CommandHolder commandHolder = null;

            // Check the state of the service.
            var currentState = caller?.GetState();
            var allowedStates = new[] {ServiceState.Running, ServiceState.Restarting};
            if (currentState != null && !allowedStates.Any(x => x == currentState))
            {
                _logger.LogInformation("The service state prohibited a proccess from running.");
                throw new InvalidOperationException($"Service state was {currentState}, invocation can NOT continue.");
            }

            // Validate that the provided directory exists.
            if (processOptions.WorkingDirectory != null)
                if (!Directory.Exists(processOptions.WorkingDirectory))
                    throw WorkingDirectoryDoesntExistException(processOptions.WorkingDirectory);


            // Check if we should run as root/admin.
            if (!processOptions.InvokeAsSuperuser)
            {
                // Nope - platform independent specification, go wild.
                commandHolder = new CommandHolder
                {
                    Options = processOptions,
                    Caller = caller,
                    Command = new Shell(
                        e => e.ThrowOnError(processOptions.ThrowOnError)
                    ).Run(
                        executable: processOptions.Executable,
                        arguments: processOptions.Arguments,
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
                    // WINDOWS =====
                    // runas verb, build the command holder with the runas verb.
                    commandHolder = new CommandHolder
                    {
                        Options = processOptions,
                        Caller = caller,
                        Command = new Shell(
                            e => e.ThrowOnError(processOptions.ThrowOnError)
                        ).Run(
                            executable: processOptions.Executable,
                            arguments: processOptions.Arguments,
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
                    // LINUX/MACOS =====

                    // Build the argument array.
                    var arguments = new List<string>();
                    arguments.Add(processOptions.Executable);
                    for (var i = 0; i != processOptions.Arguments.Length; i++)
                        arguments.Add(processOptions.Arguments[i] ?? "");
                    var procArgStr = string.Join(" ", arguments);

                    // Write to the console.
                    _logger.LogDebug("Built linux specific superuser argument array: " + procArgStr);

                    // Build the command holder with a sudo as the executable.
                    commandHolder = new CommandHolder
                    {
                        Options = processOptions,
                        Caller = caller,
                        Command = new Shell(
                            e => e.ThrowOnError(processOptions.ThrowOnError)
                        ).Run(
                            executable: GetSudoPath(),
                            arguments: arguments,
                            options: o => o
                                .DisposeOnExit(processOptions.DisposeOnExit)
                                .WorkingDirectory(processOptions.WorkingDirectory)
                        )
                    };
                }
            }

            // Attach Loggers if needed.
            _defaultProcessLogger.StartLoggingIfEnabled(commandHolder, "Start");

            // Check if we should monitor.
            if (commandHolder.Options.Monitor && caller != null)
            {
                var monitoringThread = new Thread(() => Monitor(commandHolder, caller));
                commandHolder.MonitoringThread = monitoringThread;
                monitoringThread.Start();
            }

            // Keep track of the object.
            Track(commandHolder);

            // Return
            return commandHolder;
        }

        public void Monitor(CommandHolder commandHolder, IProcessTrackable service)
        {
            // Wait for it to be tracked.
            while (!_runningCommands.Contains(commandHolder)) Thread.Sleep(100);

            // While we should track the process.
            while (_runningCommands.Contains(commandHolder))
            {
                // Check if we should dispose.
                if (commandHolder.Options.DisposeOnExit)
                {
                    // Make sure the service is still running
                    if (service.GetState() == ServiceState.Running)
                    {
                        // Check to see if the process has exited gracefully
                        if (commandHolder.Command.Process.HasExited)
                        {
                            _logger.LogWarning("A process has exited gracefully.");

                            // Stop tracking the command.
                            _runningCommands.Remove(commandHolder);
                        }
                    }
                    else
                    {
                        // Check if the command has not exited.
                        if (!commandHolder.Command.Process.HasExited)
                        {
                            // tell the console
                            _logger.LogWarning(
                                string.Format(
                                    "The service state has changed, Process ID {0} will be killed.",
                                    commandHolder.Command.Process.Id
                                )
                            );

                            // Gracefully close the process
                            commandHolder.Command.Process.Close();

                            // Stop tracking the command.
                            Untrack(commandHolder);
                        }
                    }
                }
                else // if process not dispose on exit
                {
                    if (service.GetState() == ServiceState.Running)
                    {
                        if (commandHolder.Command.Process.HasExited)
                        {
                            // TODO: address DAEM-112
                            // Tell the console.
                            _logger.LogWarning(
                                "A process has exited unexpectedly, and will be restarted. Command output redirection will cease (DAEM-112)."
                            );

                            // Restart the process.
                            commandHolder.Command.Process.Start();

                            // Attach the logger if needed.
                            _defaultProcessLogger.StartLoggingIfEnabled(commandHolder, "Monitor");
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

            _logger.LogDebug($"Monitor Thread {Thread.CurrentThread.ManagedThreadId}: Command ({commandHolder.Options.Executable}) is no longer tracked, terminating...");
        }

        /// <summary>
        /// Start tracking a command.
        /// </summary>
        /// <param name="commandHolder"></param>
        private void Track(CommandHolder commandHolder) => _runningCommands.Add(commandHolder);

        /// <summary>
        /// Stop tracking a process.
        /// </summary>
        /// <param name="commandHolder"></param>
        /// <returns></returns>
        private bool Untrack(CommandHolder commandHolder)
        {
            // Check to see if the process is already being tracked.
            if (!_runningCommands.Contains(commandHolder)) return false;
            
            // The process is being tracked, remove it.
            _runningCommands.Remove(commandHolder);
            return true;
        }

        /// <summary>
        /// Get a list of the running command holders.
        /// </summary>
        /// <returns></returns>
        public List<CommandHolder> GetRunningCommands() => _runningCommands;

        /// <summary>
        /// Get the IProcessTrackable caller object of the provided CommandHolder.
        /// </summary>
        /// <param name="referencedCommandHolder"></param>
        /// <returns></returns>
        public IProcessTrackable GetCommandCallerService(CommandHolder referencedCommandHolder) =>
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
            {
                commandHolder.Command.Process.Close();
            }
        }

        public void CloseAllBelongingToService(IProcessTrackable service, bool force = false)
        {
            // This is a particularly bad (and heavy) implementation. We should probably keep a Map<IProcessTrackable, CommandHolder> to make this easier.
            // Not bothering with it right now, however.

            // Why this? Because you cannot operate on the same Collection you're iterating.
            var iterator = _runningCommands.ToArray();

            foreach (var holder in iterator)
            {
                if (holder.Caller == null || holder.Caller.GetType() != service.GetType()) continue;

                // If we have a match, (gently, or harshly ┐(´∀｀)┌ﾔﾚﾔﾚ) close the process and remove the element from the running commands list.
                if (force)
                    holder.Command?.Kill();
                else
                    holder.Command?.Process?.Close();


                // Finally, remove it from the list of running commands.
                _runningCommands.Remove(holder);
            }
        }


        /// <summary>
        /// Terminate all tracked processes forcefully.
        /// </summary>
        public void TerminateAllTrackedCommands()
        {
            _logger.LogDebug($"Terminating {_runningCommands.Count} tracked commands");
            foreach (var commandHolder in _runningCommands)
            {
                commandHolder.Command.Kill();
            }
        }

        /// <summary>
        /// Start all the commands in the class list.
        /// This function is meant to only be called internally.
        /// </summary>
        private void StartAllTrackedCommands()
        {
            foreach (var commandHolder in _runningCommands)
            {
                commandHolder.Command.Process.Start();
                
                _defaultProcessLogger.StartLoggingIfEnabled(commandHolder, "StartAllTrackedCommands");
            }
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
        /// Throw this when the working directory does not exist.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        private static Exception WorkingDirectoryDoesntExistException(string workingDirectory) =>
            new Exception($"The WorkingDirectory attribute value provided ({workingDirectory}) did not exist.");
        
        private static string _sudoPath;

        private static string GetSudoPath()
        {
            // If the path hasn't previously been called, find it.
            if (_sudoPath == null)
            {
                var cmd = Command.Run("which", "sudo");
                _sudoPath = cmd.StandardOutput.ReadToEnd().Trim();
            }

            // Return the path to the sudo binary.
            return _sudoPath;
        }
    }
}