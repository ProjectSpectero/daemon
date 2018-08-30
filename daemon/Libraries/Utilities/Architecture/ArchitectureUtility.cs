using System;
using System.IO;
using Medallion.Shell;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Utilities.Architecture
{
    public class ArchitectureUtility : IArchitectureUtility
    {
        private readonly IProcessRunner _processRunner;

        public ArchitectureUtility(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
        }

        public string GetArchitecture()
        {
            if (AppConfig.isWindows) return GetWindowsArchitecture();
            if (AppConfig.isLinux) return GetLinuxArchitecture();
            if (AppConfig.isUnix) return GetUnixArchitecture();
            return null;
        }

        /// <summary>
        /// It is important to know that BSD/Unix like OS X does not have uname -a
        /// thus, needs it's own function.
        /// </summary>
        /// <returns></returns>
        public string GetUnixArchitecture()
        {
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
            // Options for the uname call
            var procOptions = new ProcessOptions
            {
                Executable = "uname",
                Arguments = new[] {"-m"},
                Monitor = false
            };

            // Try to execute uname
            try
            {
                var proc = _processRunner.Run(procOptions);
                proc.Command.Wait();
                var cmdOut = proc.Command.StandardOutput.ReadToEnd().Trim();
                return cmdOut;
            }
            catch (ErrorExitCodeException exception)
            {
                // Running the command manually failed - get creative and check if we're on a raspberry pi.
                return File.Exists("/proc/device-tree/model") ? "armv7l" : "x86_64";
            }
        }

        /// <summary>
        /// Windows should be running some form of x86, thus this ternary operator should work.
        /// </summary>
        /// <returns></returns>
        public string GetWindowsArchitecture()
        {
            return System.Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
        }
    }
}