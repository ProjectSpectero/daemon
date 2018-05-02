using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Spectero.daemon.Libraries.APM
{
    public class LinuxEnvironment : ISystemEnvironment
    {
        // Cache Vars - will hold old data until explicitly refreshed.
        private Dictionary<string, string> _cachedProcCpuinfo;
        private Dictionary<string, long> _cachedProcMeminfo;
        private int _threadCount;

        public LinuxEnvironment()
        {
            ReadProcMeminfo();
            ReadProcCpuinfo();
        }

        /// <summary>
        /// Returns the Processor Manufacturer, Model and the Frequency.
        /// </summary>
        /// <returns></returns>
        public string GetCpuName() =>
            ReadProcCpuinfo()["model name"];

        /// <summary>
        /// Returns the number of physical cores excluding threads.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() =>
            int.Parse(ReadProcCpuinfo()["cpu cores"]);

        /// <summary>
        /// Returns the number of threads.
        /// </summary>
        /// <returns></returns>
        public int GetCpuThreadCount() =>
            _threadCount;

        /// <summary>
        /// Returns the cache size of the processor.
        /// </summary>
        /// <returns></returns>
        public object GetCpuCacheSize() =>
            ReadProcCpuinfo()["cache size"];

        /// <summary>
        /// It should be worth noting according to linux, that free memory is marked as "used" due to buffers and caches.
        /// MemAvailable is an alternative that shows memory that can actually be utilized.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryFree() =>
            ReadProcMeminfo()["MemAvailable"];

        /// <summary>
        /// Gets the total amount of physical memory in the system.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            ReadProcMeminfo()["MemTotal"];

        /// <summary>
        /// Get Physical Memory used
        ///
        /// This function does not take account for cache and buffers.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryUsed() =>
            GetPhysicalMemoryTotal() - GetPhysicalMemoryFree();

        /// <summary>
        /// This will purge the cahced value for the CPU Information.
        /// </summary>
        public void PurgeCachedProcCpuinfo()
        {
            _cachedProcCpuinfo = null;
        }

        /// <summary>
        /// This will purge the cached value for the Memory Information.
        /// </summary>
        public void PurgeCachedProcMeminfo()
        {
            _cachedProcMeminfo = null;
        }

        /// <summary>
        /// Read /proc/meminfo
        ///
        /// This gets the memory informtaion of the current system
        ///
        /// Explanation of cache variables:
        /// This fuction idealy will be called multiple times, and if the data is refreshed the values will not add up each call.
        /// By caching the result, we can specifically for sure know that the data we are reading from is the same.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, long> ReadProcMeminfo(bool clearCache = false)
        {
            // Check if we should clear the cache or if the cache even is populated.
            if (clearCache || _cachedProcMeminfo == null)
            {
                // Read the file line by line into a string array.
                string[] readProcInformation = File.ReadAllLines("/proc/meminfo");

                // Placeholder dictionary.
                var procInfo = new Dictionary<string, long>();

                // Iterate through each line.
                foreach (string procLine in readProcInformation)
                {
                    // Splitting the string based on the colon (Example: => CommitLimit: 12205572 kB)
                    string[] procPart = procLine.Split(":");

                    // Verbose for the sake of understanding.
                    string key = procPart[0];
                    string value = procPart[1].TrimStart(' ');
                    long parsedValue;

                    // Convert to bytes if contains kB
                    if (value.Contains(" kB"))
                    {
                        value = value.Substring(0, value.Length - 4);
                        parsedValue = long.Parse(value) * 1024;
                    }
                    else
                    {
                        parsedValue = long.Parse(value);
                    }

                    // Append to the dictionary.
                    procInfo.Add(key, parsedValue);
                }

                // Write to the cache and return.
                _cachedProcMeminfo = procInfo;
            }

            // Return the cached value.
            return _cachedProcMeminfo;
        }

        /// <summary>
        /// Read /proc/cpuinfo
        ///
        /// This gets the cpu informtaion of the current system
        ///
        /// Explanation of cache variables:
        /// This fuction idealy will be called multiple times, and if the data is refreshed the values will not add up each call.
        /// By caching the result, we can specifically for sure know that the data we are reading from is the same.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> ReadProcCpuinfo(bool clearCache = false)
        {
            // Check if we should clear the cache or if the cache even is populated.
            if (clearCache || _cachedProcCpuinfo == null)
            {
                // Read the file line by line into a string array.
                string[] readProcInformation = File.ReadAllLines("/proc/cpuinfo");

                // Placeholder dictionary.
                var procInfo = new Dictionary<string, string>();

                // Iterate through each line.
                foreach (var procLine in readProcInformation)
                {
                    // Splitting the string based on the colon (Example: => CommitLimit: 12205572 kB)
                    string[] procPart = procLine.Split(":");

                    // Verbose for the sake of understanding.
                    string key = procPart[0].TrimEnd(' ');
                    string value = procPart[1];

                    // Keep track of the number of threads.
                    if (key == "processor") _threadCount++;

                    // If the key already exists, ignore.
                    if (!procInfo.ContainsKey(key))
                    {
                        // Check if we need to edit certian objects.
                        switch (key)
                        {
                            case "model name":
                                // Truncate the string if it contains more than two spaces between a segment.
                                value = new Regex("[ ]{2,}", RegexOptions.None).Replace(value, "");
                                break;
                        }
                    }

                    // Append to the dictionary.
                    procInfo.Add(key, value);
                }

                // Write to the cache and return.
                _cachedProcCpuinfo = procInfo;
            }

            // Return the cached value.
            return _cachedProcCpuinfo;
        }

        /// <summary>
        /// Get information about the memory on the system in the form of a dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetMemoryDetails()
        {
            // Purge any cached proc information.
            PurgeCachedProcMeminfo();

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
            // Purge any cached proc information.
            PurgeCachedProcCpuinfo();

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
    }
}