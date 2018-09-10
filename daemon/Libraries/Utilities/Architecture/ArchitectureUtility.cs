using System;
using System.IO;
using Hangfire.Logging;
using Medallion.Shell;
using Microsoft.Extensions.Logging;
using NLog;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;
using ILogger = NLog.ILogger;

namespace Spectero.daemon.Libraries.Utilities.Architecture
{
    public class ArchitectureUtility : IArchitectureUtility
    {
        private readonly IProcessRunner _processRunner;
        private readonly ILogger<ArchitectureUtility> _logger;

        public ArchitectureUtility(ILogger<ArchitectureUtility> logger, IProcessRunner processRunner)
        {
            _logger = logger;
            _processRunner = processRunner;
        }

        public string GetArchitecture()
        {
            // Architecture gathered placeholder
            string returnableString = null;

            // Test each OS.
            if (AppConfig.isWindows) returnableString = GetWindowsArchitecture();
            if (AppConfig.isLinux) returnableString = GetLinuxArchitecture();
            if (AppConfig.isUnix) returnableString = GetUnixArchitecture();

            if (returnableString != null)
            {
                // Log:
                _logger.LogInformation($"AU: The system architecture is {returnableString}.");
                return returnableString;
            }
            else
            {
                var err = "The ArchitectureUtility failed to gather a architecture for this operating system.";
                _logger.LogWarning(err);
                throw new InternalError(err);
            }
        }

        /// <summary>
        /// It is important to know that BSD/Unix like OS X does not have uname -a
        /// thus, needs it's own function.
        /// </summary>
        /// <returns></returns>
        public string GetUnixArchitecture()
        {
            // Identification
            _logger.LogDebug("AU: The system environment appears to be Unix/MacOS.");

            // Not implemented - return error.
            throw new NotImplementedException("There is currently no support for this operating system.");
        }

        /// <summary>
        /// Attempt to get the architecture from the machine programatically using kernel utilities
        /// In the event that uname fails, it will fail over on determining if it is a raspberry pi
        ///
        /// This should eventually be re-worked to a better and more robust implementation.
        /// </summary>
        /// <returns></returns>
        public string GetLinuxArchitecture()
        {
            // Identification
            _logger.LogDebug("AU: The system environment appears to be Linux.");

            // Options for the uname call
            var procOptions = new ProcessOptions
            {
                Executable = "uname",
                Arguments = new[] {"-m"},
                Monitor = false
            };

            try
            {
                // Try to run the command.
                var proc = _processRunner.Run(procOptions);

                // Make sure it exits properly.
                proc.Command.Wait();

                // Trim the result and return.
                var cmdOut = proc.Command.StandardOutput.ReadToEnd().Trim();
                return cmdOut;
            }
            catch (ErrorExitCodeException exception)
            {
                try
                {
                    // Running the command manually failed - get creative and check if we're on a raspberry pi.
                    var boolResult = File.Exists("/proc/device-tree/model") ? "armv7l" : "x86_64";

                    // If the file exists, we're a raspberry pi and thus some ARM.
                    return boolResult;
                }
                catch (FileNotFoundException notFoundException)
                {
                    //!!! At this point, we've failed to determine the architecture and should just throw the exception.


                    var err = "AU: Failed to find the architecture for linux operating system.";
                    _logger.LogWarning(err);
                    throw new InternalError(err);
                }
            }
        }

        /// <summary>
        /// Windows should be running some form of x86, thus this ternary operator should work.
        /// </summary>
        /// <returns></returns>
        public string GetWindowsArchitecture()
        {
            // Identification
            _logger.LogDebug("AU: The system environment appears to be Windows.");

            // Return the data we want.
            return Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
        }
    }
}