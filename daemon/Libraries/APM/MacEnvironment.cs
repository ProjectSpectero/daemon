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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Medallion.Shell;

namespace Spectero.daemon.Libraries.APM
{
    public class MacEnvironment : ISystemEnvironment
    {
        private Dictionary<string, string> _cachedSysctlOutput;
        private Dictionary<string, long> _cachedVmStatOutput;
        private int _pageSize;

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
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryUsed() =>
            GetVmStatOutput()["Pages active"] * _pageSize;

        /// <summary>
        /// Get the physical amount of memory free
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryFree() =>
            GetVmStatOutput()["Pages active"] * _pageSize;

        /// <summary>
        /// Get the total amount of RAM the system has in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            long.Parse(GetSysctlOutput()["hw.memsize"]);

        /// <summary>
        /// Delete all cached objects.
        /// </summary>
        public void PurgeCachedInformation()
        {
            _cachedVmStatOutput = null;
            _cachedSysctlOutput = null;
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
                        if (segements.Count() >= 2)
                            sysctlOutput.Add(segements[0].Trim(), segements[1].Trim());
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
                    // Determine the page size
                    if (row.Contains("page size of "))
                    {
                        _pageSize = int.Parse(Regex.Replace(row, @"[^\d]", "")); // Get the numbers only.
                    }
                    else if (row.Contains(":")) // Dictionarize the value.
                    {
                        string[] segements = row.Split(":");
                        if (segements.Count() >= 2)
                            vmstatOutput.Add(
                                segements[0].Trim(), // The Key of the dictionary.
                                long.Parse(Regex.Replace(segements[1], @"[^\d]", "")) // The Value, number sorted.
                            );
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