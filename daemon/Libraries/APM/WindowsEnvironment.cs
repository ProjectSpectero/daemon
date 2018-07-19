using System;
using System.Collections.Generic;
using System.Management;

namespace Spectero.daemon.Libraries.APM
{
    public class WindowsEnvironment : ISystemEnvironment
    {
        // Cached WMI Object to make sure the same value
        private Dictionary<string, string> _cachedOperatingSystemManagementObject;
        private Dictionary<string, string> _cachedProccessorManagementObject;

        public WindowsEnvironment()
        {
            GetWmiOperatingSystemManagementObject();
            GetWmiProcessorManagementObject();
        }

        /// <summary>
        /// Get the processor name from the environment.
        /// </summary>
        /// <returns></returns>
        public string GetCpuName() =>
            GetWmiProcessorManagementObject().GetValueOrDefault("Name", "Unknown Processor");

        /// <summary>
        /// Get the number of cores from WMI.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() =>
            int.Parse(GetWmiProcessorManagementObject().GetValueOrDefault("NumberOfCores", "1"));

        /// <summary>
        /// Get the number of threads.
        ///
        /// It should be worth noting this is the "logical" processor count on windows.
        /// </summary>
        /// <returns></returns>
        public int GetCpuThreadCount() =>
            int.Parse(GetWmiProcessorManagementObject().GetValueOrDefault("NumberOfLogicalProcessors", "1"));

        /// <summary>
        /// Get L2 Cache Size in Kilobytes.
        ///
        /// Read from WMI to get the size of the cache.
        /// </summary>
        /// <returns></returns>
        public object GetCpuCacheSize() =>
            uint.Parse(GetWmiProcessorManagementObject().GetValueOrDefault("L2CacheSize", "0"));

        /// <summary>
        /// Get the physical amount of memory used in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryUsed() =>
            GetPhysicalMemoryTotal() - GetPhysicalMemoryFree();

        /// <summary>
        /// Get the physical amount of memory free in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryFree() =>
            long.Parse(GetWmiOperatingSystemManagementObject().GetValueOrDefault("FreePhysicalMemory", "0")) * 1024;

        /// <summary>
        /// Get the total amount of physical memory in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            long.Parse(GetWmiOperatingSystemManagementObject().GetValueOrDefault("TotalVisibleMemorySize", "0")) * 1024;

        /// <summary>
        /// Get the amount of virtual memory used in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetVirtualMemoryUsed() =>
            GetVirtualMemoryTotal() - GetVirtualMemoryFree();

        /// <summary>
        /// Get the amount of virtual memory free in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetVirtualMemoryFree() =>
            long.Parse(GetWmiOperatingSystemManagementObject().GetValueOrDefault("FreeVirtualMemory", "0")) * 1024;

        /// <summary>
        /// Get the total amount of virtual memory in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetVirtualMemoryTotal() =>
            long.Parse(GetWmiOperatingSystemManagementObject().GetValueOrDefault("TotalVirtualMemorySize", "0")) * 1024;

        /// <summary>
        /// Delete all cached objects.
        /// </summary>
        public void PurgeCachedInformation()
        {
            _cachedOperatingSystemManagementObject = null;
            _cachedProccessorManagementObject = null;
        }

        public Dictionary<string, string> GetWmiOperatingSystemManagementObject(bool clearCache = false)
        {
            if (clearCache || _cachedOperatingSystemManagementObject == null)
            {
                // Instantiate a new dictionary.
                var localDictionary = new Dictionary<string, string>();

                // Prepare the query.
                var query = new SelectQuery(@"SELECT * FROM Win32_OperatingSystem");

                try
                {
                    // Search
                    using (var searcher = new ManagementObjectSearcher(query))
                    {
                        // Execute and iterate, then append to dictionary.
                        foreach (var currentManagementObject in searcher.Get())
                        foreach (var prop in currentManagementObject.Properties)
                        {
                            try
                            {
                                localDictionary.Add(prop.Name.Trim(), prop.Value.ToString().Trim());
                            }
                            catch (Exception exception)
                            {
                                // Pass, there's nothing that needs to be done.
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // WMI is not ready, return 
                }

                // Assign to cache.
                _cachedOperatingSystemManagementObject = localDictionary;
            }

            // Return the cached value regardless.
            return _cachedOperatingSystemManagementObject;
        }

        public Dictionary<string, string> GetWmiProcessorManagementObject(bool clearCache = false)
        {
            if (clearCache || _cachedProccessorManagementObject == null)
            {
                // Instantiate a new dictionary.
                var localDictionary = new Dictionary<string, string>();

                // Prepare the query.
                var query = new SelectQuery(@"SELECT * FROM Win32_Processor");

                try
                {
                    // Search
                    using (var searcher = new ManagementObjectSearcher(query))
                    {
                        // Execute and iterate, then append to dictionary.
                        foreach (var currentManagementObject in searcher.Get())
                        foreach (var prop in currentManagementObject.Properties)
                        {
                            try
                            {
                                localDictionary.Add(prop.Name.Trim(), prop.Value.ToString().Trim());
                            }
                            catch (Exception exception)
                            {
                                // Pass, there's nothing that needs to be done.
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // WMI is not ready, return 
                }

                // Assign to cache.
                _cachedProccessorManagementObject = localDictionary;
            }

            // Return the cached value regardless.
            return _cachedProccessorManagementObject;
        }
    }
}