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
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public class ProcessLogger
    {
        private readonly ILogger<object> _logger;
        
        public ProcessLogger(ILogger<object> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Attach the command to all available stream readers.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        public void LatchQuickly(ILogger<ProcessRunner> logger, CommandHolder commandHolder)
        {
            // Start the standard stream reader.
            new Thread(() => { Standard(commandHolder); }).Start();

            // Start the error stream reader.
            new Thread(() => { Error(commandHolder); }).Start();
        }
        

        /// <summary>
        /// Attach the command to the standard stream reader.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        private async void Standard(CommandHolder commandHolder)
        {
            string line;
            while ((line = await commandHolder.Command.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                commandHolder.Options.streamProcessor.StandardOutputProcessor(line, commandHolder);
        }

        /// <summary>
        /// Attach the command to the error stream reader.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        private async void Error(CommandHolder commandHolder)
        {
            string line;
            while ((line = await commandHolder.Command.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
                commandHolder.Options.streamProcessor.ErrorOutputProcessor(line, commandHolder);
        }

        private void ActuallyLogInfo(string line, CommandHolder holder)
        {
            _logger.LogInformation($"{holder.Options.Executable} ({holder.Command.ProcessId} by {holder.Caller.GetType().Name}): {line}");
        }

        private void ActuallyLogError(string line, CommandHolder holder)
        {
            _logger.LogError($"{holder.Options.Executable} ({holder.Command.ProcessId} by {holder.Caller.GetType().Name}): {line}");
        }

        private StreamProcessor GetDefaultStreamProcessor()
        {
            return new StreamProcessor()
            {
                StandardOutputProcessor = ActuallyLogInfo,
                ErrorOutputProcessor = ActuallyLogError
            };
        }
        
        
        public void StartLoggingIfEnabled(CommandHolder commandHolder, string parent)
        {
            _logger.LogDebug(
                "{0} Object has requested attaching logger to command: {1}",
                parent,
                commandHolder.Options.Executable
            );
            
            if (commandHolder.Options.AttachLogToConsole)
            {
                var streamProcessor = commandHolder.Options.streamProcessor;

                if (streamProcessor?.ErrorOutputProcessor == null || streamProcessor.StandardOutputProcessor == null)
                {
                    _logger.LogDebug("Attaching default stream processor since one was not provided in the process options");
                    commandHolder.Options.streamProcessor = GetDefaultStreamProcessor();
                }
            
                Standard(commandHolder);
                Error(commandHolder);
            }
            else
                _logger.LogDebug("Process options dictate no logging is possible, ignoring...");
        }
    }
}