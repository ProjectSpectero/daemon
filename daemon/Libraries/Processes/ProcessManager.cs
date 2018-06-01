using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Command = Medallion.Shell.Command;

namespace Spectero.daemon.Libraries.Processes
{
    public class ProcessManager
    {
        private string _initializer;
        private readonly ILogger<object> _logger;
        private List<Command> _listOfCommands = new List<Command>();

        /// <summary>
        /// Class Constructor with dependency injection.
        /// </summary>
        /// <param name="initializer"></param>
        /// <param name="logger"></param>
        public ProcessManager(string initializer, ILogger<object> logger)
        {
            // Inherit dependency injection.
            _initializer = initializer;
            _logger = logger;

            // Log to the console.
            logger.LogInformation("Process Manager Initialized for " + _initializer);
        }

        /// <summary>
        /// Start tracking a process.
        /// </summary>
        /// <param name="referencedCommand"></param>
        public void Track(Command referencedCommand) => _listOfCommands.Add(referencedCommand);

        /// <summary>
        /// Start tracking a process only if it's not already tracked.
        /// </summary>
        /// <param name="referencedCommand"></param>
        public void SafeTrack(Command referencedCommand) => _listOfCommands.AddIfNotExists(referencedCommand);

        /// <summary>
        /// Start tracking a list of processes
        /// </summary>
        /// <param name="referencedCommandList"></param>
        public void Track(List<Command> referencedCommandList)
        {
            foreach (var command in referencedCommandList)
                _listOfCommands.Add(command);
        }

        /// <summary>
        /// Start tracking a list of processes and onle add those that aren't already tracked.
        /// </summary>
        /// <param name="referencedCommandList"></param>
        public void SafeTrack(List<Command> referencedCommandList)
        {
            foreach (var command in referencedCommandList)
                _listOfCommands.Add(command);
        }

        /// <summary>
        /// Stop tracking a process.
        /// </summary>
        /// <param name="referencedCommand"></param>
        /// <returns></returns>
        public bool Untrack(Command referencedCommand)
        {
            // Check to see if the process is already being tracked.
            if (_listOfCommands.Contains(referencedCommand))
            {
                // The process is being tracked, remove it.
                _listOfCommands.Remove(referencedCommand);
                return true;
            }
            else
            {
                // The process is not being tracked.
                return false;
            }
        }

        /// <summary>
        /// Get a list of the tracked processes.
        /// </summary>
        /// <returns></returns>
        public List<Command> GetTrackedProcesses() => _listOfCommands;

        /// <summary>
        /// Forget all of the tracked processes.
        /// The processes that were previously tracked will still retain their state, just forgotten.
        /// </summary>
        public void ClearTrackedProcesses()
        {
            // Empty the list
            _listOfCommands = new List<Command>();

            _logger.LogInformation(string.Format("{0}'s Process Manager has forgotten it's contents.",
                _initializer));
        }

        /// <summary>
        /// close all tracked processes gracefully.
        /// </summary>
        public void CloseAllTrackedProcesses()
        {
            foreach (var command in GetTrackedProcesses())
                command.Process.Close();
            _logger.LogInformation(string.Format("{0}'s Process Manager has gracefully closed all processes.",
                _initializer));
        }

        /// <summary>
        /// Terminate all tracked processes forcefully.
        /// </summary>
        public void TerminateAllTrackedProcesses()
        {
            foreach (var command in GetTrackedProcesses())
                command.Process.Kill();
            _logger.LogInformation(string.Format("{0}'s Process Manager has forcefully closed all processes.",
                _initializer));
        }

        /// <summary>
        /// Start all the processes in the class list.
        /// This function is meant to only be called internally.
        /// </summary>
        private void StartAllTrackedProcesses()
        {
            foreach (var command in GetTrackedProcesses())
                command.Process.Start();
            _logger.LogInformation(string.Format("{0}'s Process Manager has successfully started all processes.",
                _initializer));
        }

        /// <summary>
        /// Restart all tracked processes.
        /// </summary>
        public void RestartAllTrackedProcesses(bool force = false)
        {
            _logger.LogInformation(string.Format("{0}'s Process Manager was instructed to restart all processes.",
                _initializer));

            // Check if we should agressively close all the processes.
            if (!force)
                // Safely.
                CloseAllTrackedProcesses();
            else
                // Aggressive.
                TerminateAllTrackedProcesses();

            // Start them all again.
            StartAllTrackedProcesses();
        }

        /// <summary>
        /// Set the reference name of the reason why this ProcessManager was created.
        /// </summary>
        /// <param name="initializer"></param>
        public void SetInitializer(string initializer)
        {
            _initializer = initializer;
            _logger.LogInformation("Initializer has changed to " + initializer);
        }

        /// <summary>
        /// Get the reference name of the reason why the ProcessManager was created.
        /// </summary>
        /// <returns></returns>
        public string GetInitializer() => _initializer;
    }
}