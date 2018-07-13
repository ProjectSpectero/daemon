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
        private int _threadCount = 0;

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
            ReadProcCpuinfo().GetValueOrDefault("model name", "Unknown Processor");

        /// <summary>
        /// Returns the number of physical cores excluding threads.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() =>
            int.Parse(ReadProcCpuinfo().GetValueOrDefault("cpu cores", "1"));

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
            ReadProcCpuinfo().GetValueOrDefault("cache size", "Unknown");

        /// <summary>
        /// It should be worth noting according to linux, that free memory is marked as "used" due to buffers and caches.
        /// MemAvailable is an alternative that shows memory that can actually be utilized.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryFree() =>
            ReadProcMeminfo().GetValueOrDefault(
                "MemAvailable",
                ReadProcMeminfo().GetValueOrDefault("MemFree", 0) +
                ReadProcMeminfo().GetValueOrDefault("Cached", 0)
            );

        /// <summary>
        /// Gets the total amount of physical memory in the system.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            ReadProcMeminfo().GetValueOrDefault("MemTotal", 0);

        /// <summary>
        /// Get Physical Memory used
        ///
        /// This function does not take account for cache and buffers.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryUsed() =>
            GetPhysicalMemoryTotal() - GetPhysicalMemoryFree();

        /// <summary>
        /// Delete all cached objects.
        /// </summary>
        public void PurgeCachedInformation()
        {
            _cachedProcCpuinfo = null;
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
                    string key = procPart[0].Trim();
                    string unregexedValue = procPart[1].Trim();
                    string regexedValue = Regex.Replace(unregexedValue, @"[^\d]", "") ?? unregexedValue;

                    // Convert to bytes if contains kB
                    if (unregexedValue.Contains(" kB"))
                    {
                        // Variable where conversion to bytes happens.
                        long valueToAppend = long.Parse(regexedValue) * 1024;

                        // Add to dictionary.
                        procInfo.Add(key, valueToAppend);
                    }
                    else
                    {
                        // Add to dictionary.
                        procInfo.Add(key, long.Parse(unregexedValue));
                    }
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
                    if (procLine.Contains(":"))
                    {
                        // Splitting the string based on the colon (Example: => CommitLimit: 12205572 kB)
                        string[] procPart = procLine.Split(":");

                        // Verbose for the sake of understanding.
                        string key = procPart[0].Trim();
                        string value = procPart[1].Trim();

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

                            // Append to the dictionary.
                            procInfo.Add(key, value);
                        }
                    }
                }

                // Write to the cache and return.
                _cachedProcCpuinfo = procInfo;
            }

            // Return the cached value.
            return _cachedProcCpuinfo;
        }
    }
}