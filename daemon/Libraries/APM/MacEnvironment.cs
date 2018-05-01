using System;
using System.Collections.Generic;
using System.Linq;
using Medallion.Shell;

namespace Spectero.daemon.Libraries.APM
{
    public class MacEnvironment : ISystemEnvironment
    {
        private Dictionary<string, string> _cachedSysctlOutput;

        public MacEnvironment()
        {
            GetSysctlOutput();
        }

        /// <summary>
        /// Returns the processor name
        /// Example: Intel(R) Core(TM) i7-7700K CPU @ 4.20GHz
        /// </summary>
        /// <returns></returns>
        public string GetCpuName() => _cachedSysctlOutput["machdep.cpu.brand_string"];

        /// <summary>
        /// Returns the physical count of the cores in the procecssor.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() => int.Parse(_cachedSysctlOutput["hw.physicalcpu"]);

        /// <summary>
        /// Returns the number of threads in the processor.
        /// </summary>
        /// <returns></returns>
        public int GetCpuThreadCount() => int.Parse(_cachedSysctlOutput["hw.logicalcpu"]);

        /// <summary>
        /// Gets the L2 Cache size of the processor.
        /// </summary>
        /// <returns></returns>
        public object GetCpuCacheSize() => _cachedSysctlOutput["machdep.cpu.cache.size"];

        public long GetPhysicalMemoryUsed()
        {
            throw new NotImplementedException();
        }

        public long GetPhysicalMemoryFree()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the total amount of RAM the system has in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() => long.Parse(_cachedSysctlOutput["hw.memsize"]);

        /// <summary>
        /// Get information about the memory on the system in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            // Get Physical Memory
            var physicalMemoryObjects = new Dictionary<string, object>()
            {
                {"Used", GetPhysicalMemoryUsed()},
                {"Free", GetPhysicalMemoryFree()},
                {"Total", GetPhysicalMemoryTotal()}
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
            // Return the compiled object.
            return new Dictionary<string, object>()
            {
                {"Model", GetCpuName()},
                {"Cores", GetCpuCoreCount()},
                {"Threads", GetCpuThreadCount()},
                {"Cache Size", GetCpuCacheSize()}
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
        /// Use Medallion Shell to get the output out of Sysctl -a
        /// </summary>
        /// <param name="clearCache"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetSysctlOutput(bool clearCache = false)
        {
            if (clearCache || _cachedSysctlOutput == null)
            {
                var sysctlOutput = new Dictionary<string, string>();

                // Run the command and get what we can out of the system environ,emt.
                var sysctlCommand = Command.Run("sysctl", "-a");

                // Iterate though each like and parse it to the format that we need.
                foreach (string row in sysctlCommand.StandardOutput.GetLines().ToList())
                {
                    string[] segements = row.Split(": ");
                    sysctlOutput.Add(segements[0], segements[1]);
                }

                // Update the cache
                _cachedSysctlOutput = sysctlOutput;
            }

            return _cachedSysctlOutput;
        }
    }
}