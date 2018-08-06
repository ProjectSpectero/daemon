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
    public class CommandLogger
    {
        /// <summary>
        /// Attach the command to all available stream readers.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        public static void LatchQuickly(ILogger<ProcessRunner> logger, CommandHolder commandHolder)
        {
            // Start the standard stream reader.
            new Thread(() => { Standard(logger, commandHolder); }).Start();

            // Start the error stream reader.
            new Thread(() => { Error(logger, commandHolder); }).Start();
        }

        /// <summary>
        /// Attach the command to the standard stream reader.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        private static async void Standard(ILogger<ProcessRunner> logger, CommandHolder commandHolder)
        {
            string line;
            while ((line = await commandHolder.Command.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                logger.LogInformation(line);
        }

        /// <summary>
        /// Attach the command to the error stream reader.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="commandHolder"></param>
        private static async void Error(ILogger<ProcessRunner> logger, CommandHolder commandHolder)
        {
            string line;
            while ((line = await commandHolder.Command.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
                logger.LogError(line);
        }

        public static Action<ILogger<ProcessRunner>, CommandHolder> StandardAction()
        {
            Action<ILogger<ProcessRunner>, CommandHolder> act = Standard;
            return act;
        }

        public static Action<ILogger<ProcessRunner>, CommandHolder> ErrorAction()
        {
            Action<ILogger<ProcessRunner>, CommandHolder> act = Error;
            return act;
        }
    }
}