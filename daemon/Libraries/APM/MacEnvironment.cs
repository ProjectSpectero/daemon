using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medallion.Shell;

namespace Spectero.daemon.Libraries.APM
{
    public class MacEnvironment : ISystemEnvironment
    {
        private Dictionary<string, object> cachedSysctlOutput;

        public MacEnvironment()
        {
        }

        public string GetCpuName()
        {
            return cachedSysctlOutput["machdep.cpu.brand_string"].ToString();
        }

        public int GetCpuCoreCount()
        {
            return int.Parse(cachedSysctlOutput["machdep.cpu.core_count"].ToString()); 
        }

        public int GetCpuThreadCount()
        {
            return int.Parse(cachedSysctlOutput["machdep.cpu.thread_count"].ToString());
        }

        public string GetCpuCacheSize()
        {
            return cachedSysctlOutput["machdep.cpu.cache.size"].ToString();
        }

        public double GetPhysicalMemoryUsed()
        {
            throw new NotImplementedException();
        }

        public double GetPhysicalMemoryFree()
        {
            throw new NotImplementedException();
        }

        public double GetPhysicalMemoryTotal()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get information about the memory on the system in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            // Get Physical Memory
            Dictionary<string, object> physicalMemoryObjects = new Dictionary<string, object>()
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

        public Dictionary<string, object> GetSysctlOutput(bool clearCache = false)
        {
            if (clearCache || cachedSysctlOutput == null)
            {
                Dictionary<string, object> sysctlOutput = new Dictionary<string, object>();

                // Run the command and get what we can out of the system environ,emt.
                Command sysctlCommand = Command.Run("sysctl", "-a machdep");

                // Iterate though each like and parse it to the format that we need.
                foreach (string row in sysctlCommand.StandardOutput.GetLines().ToList())
                {
                    string[] segements = row.Split(": ");
                    sysctlOutput.Add(segements[0], segements[1]);
                }

                // Update the cache
                cachedSysctlOutput = sysctlOutput;
            }

            return cachedSysctlOutput;
        }
    }
}