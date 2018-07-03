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