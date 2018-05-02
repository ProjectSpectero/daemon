using System;
using System.Collections.Generic;
using System.Linq;
using Medallion.Shell;

namespace Spectero.daemon.Libraries.APM
{
    public class MacEnvironment : ISystemEnvironment
    {
        private Dictionary<string, string> _cachedSysctlOutput;
        private Dictionary<string, long> _cachedVmStatOutput;

        public MacEnvironment()
        {
            GetSysctlOutput();
            GetVmStatOutput();
        }

        /// <summary>
        /// Returns the processor name
        /// Example: Intel(R) Core(TM) i7-7700K CPU @ 4.20GHz
        /// </summary>
        /// <returns></returns>
        public string GetCpuName() =>
            GetSysctlOutput()["machdep.cpu.brand_string"];

        /// <summary>
        /// Returns the physical count of the cores in the procecssor.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() =>
            int.Parse(GetSysctlOutput()["hw.physicalcpu"]);

        /// <summary>
        /// Returns the number of threads in the processor.
        /// </summary>
        /// <returns></returns>
        public int GetCpuThreadCount() =>
            int.Parse(GetSysctlOutput()["hw.logicalcpu"]);

        /// <summary>
        /// Gets the L2 Cache size of the processor.
        /// </summary>
        /// <returns></returns>
        public object GetCpuCacheSize() =>
            GetSysctlOutput()["machdep.cpu.cache.size"];

        /// <summary>
        /// Get thge physical amount of memory used
        /// Each page accounts for 4096 bytes
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryUsed() =>
            GetVmStatOutput()["Pages active"] * 4096;

        /// <summary>
        /// Get the physical amount of memory free
        /// Each page accounts for 4096 bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryFree() =>
            GetVmStatOutput()["Pages active"] * 4096;

        /// <summary>
        /// Get the total amount of RAM the system has in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            long.Parse(GetSysctlOutput()["hw.memsize"]);

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
                foreach (string line in sysctlCommand.StandardOutput.GetLines().ToList())
                {
                    if (line.Contains(":"))
                    {
                        // Split the string between the key and value.
                        string[] segements = line.Split(": ");

                        // Ensure that we don't go out of bounds.
                        if (segements.Count() >= 2) sysctlOutput.Add(segements[0].Trim(), segements[1].Trim());
                    }
                }

                // Update the cache
                _cachedSysctlOutput = sysctlOutput;
            }

            return _cachedSysctlOutput;
        }

        public Dictionary<string, long> GetVmStatOutput(bool clearCache = false)
        {
            if (clearCache || _cachedVmStatOutput == null)
            {
                var vmstatOutput = new Dictionary<string, long>();

                // Run the command and get what we can out of the system environ,emt.
                var vmstatCommand = Command.Run("vm_stat");

                // Iterate though each like and parse it to the format that we need.
                foreach (string row in vmstatCommand.StandardOutput.GetLines().ToList())
                {
                    if (row.Contains(":") && !row.Contains("page size of"))
                    {
                        string[] segements = row.Split(":");
                        vmstatOutput.Add(segements[0], long.Parse(segements[1].Trim()));
                    }
                }

                // Update the cache
                _cachedVmStatOutput = vmstatOutput;
            }

            // Return the cache regardless of operation
            return _cachedVmStatOutput;
        }
    }
}