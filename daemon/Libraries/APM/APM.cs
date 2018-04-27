using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.APM
{
    public class APM
    {
        private readonly ISystemEnvironment _operatingSystemEnvironment;

        /// <summary>
        /// Constructor
        ///
        /// Notes(Andrew):
        /// I'd much rather this be a switch case statment, but it's WET due to implementation.
        /// </summary>
        public APM()
        {
            // Check if we have a supported operating system.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _operatingSystemEnvironment = new WindowsEnvironment();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _operatingSystemEnvironment = new LinuxEnvironment();
            }
            else
            {
                // TODO(Andrew): Implement OS X Support - I don't have an OS X machine.
                // Unsupported Operating System.
                Console.WriteLine("This application is running on an unsupported operating system.");
                Console.WriteLine("Press enter/return key to exit (Exit Code: 10).");
                Console.ReadLine();
                Environment.Exit(10);
            }
        }

        /// <summary>
        /// Get all details from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetAllDetails()
        {
            return _operatingSystemEnvironment.GetAllDetails();
        }

        /// <summary>
        /// Get information about the processor from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetCpuDetails()
        {
            return _operatingSystemEnvironment.GetCpuDetails();
        }

        /// <summary>
        /// Get memory details from the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            return _operatingSystemEnvironment.GetMemoryDetails();
        }

        /// <summary>
        /// Shorthand function to get the environment details of the delegated operating system environment.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetEnvironmentDetails()
        {
            return _operatingSystemEnvironment.GetEnvironmentDetails();
        }

        /// <summary>
        /// Get the instance of the operating system environment handler.
        /// </summary>
        /// <returns></returns>
        public ISystemEnvironment GetSystemEnvironment() => _operatingSystemEnvironment;
    }
}