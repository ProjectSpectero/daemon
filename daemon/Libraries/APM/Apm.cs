using System;
using System.Collections.Generic;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Libraries.APM
{
    public class Apm
    {
        private readonly ISystemEnvironment _operatingSystemEnvironment;

        public Apm()
        {
            // Check if we have a supported operating system.
            if (AppConfig.isWindows)
            {
                _operatingSystemEnvironment = new WindowsEnvironment();
            }
            else if (AppConfig.isLinux)
            {
                _operatingSystemEnvironment = new LinuxEnvironment();
            }
            else if (AppConfig.isMac)
            {
                _operatingSystemEnvironment = new MacEnvironment();
            }
        }

        /// <summary>
        /// Get information about the memory on the system in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            // Purge any cached proc information.
            _operatingSystemEnvironment.PurgeCachedInformation();

            // Get Physical Memory
            var physicalMemoryObjects = new Dictionary<string, object>()
            {
                {"Used", _operatingSystemEnvironment.GetPhysicalMemoryUsed()},
                {"Free", _operatingSystemEnvironment.GetPhysicalMemoryFree()},
                {"Total", _operatingSystemEnvironment.GetPhysicalMemoryTotal()}
            };

            // Returned the compiled object.
            return new Dictionary<string, object>()
            {
                {"Physical", physicalMemoryObjects},
            };
        }

        /// <summary>
        /// Get infomration about the CPU in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetCpuDetails()
        {
            // Purge any cached proc information.
            _operatingSystemEnvironment.PurgeCachedInformation();

            // Return the compiled object.
            return new Dictionary<string, object>()
            {
                {"Model", _operatingSystemEnvironment.GetCpuName()},
                {"Cores", _operatingSystemEnvironment.GetCpuCoreCount()},
                {"Threads", _operatingSystemEnvironment.GetCpuThreadCount()},
                {"Cache Size", _operatingSystemEnvironment.GetCpuCacheSize()}
            };
        }

        /// <summary>
        /// Get infoirmation about the environment in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetEnvironmentDetails()
        {
            return new Dictionary<string, object>()
            {
                {"Hostname", Environment.MachineName},
                {"OS Version", Environment.OSVersion},
                {"64-Bits", Is64Bits()}
            };
        }

        /// <summary>
        /// Get all environment information in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetAllDetails()
        {
            return new Dictionary<string, object>()
            {
                {"CPU", GetCpuDetails()},
                {"Memory", GetMemoryDetails()},
                {"Environment", GetEnvironmentDetails()}
            };
        }

        /// <summary>
        /// Determine if the system is running in 64 bit mode.
        /// </summary>
        /// <returns></returns>
        public bool Is64Bits()
        {
            return Environment.Is64BitOperatingSystem;
        }

        /// <summary>
        /// Get the instance of the operating system environment handler.
        ///
        /// This is extendable: Apm.SystemEnvironment().GetCpuDetails();
        /// </summary>
        /// <returns></returns>
        public ISystemEnvironment SystemEnvironment() => _operatingSystemEnvironment;
    }
}