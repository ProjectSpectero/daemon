using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.APM
{
    public class WindowsEnvironment : ISystemEnvironment
    {
        // Cached WMI Object to make sure the same value
        private ManagementObject _cachedOperatingSystemManagementObject;
        private ManagementObject _cachedProccessorManagementObject;

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
            GetWmiProcessorManagementObject()["Name"].ToString();

        /// <summary>
        /// Get the number of cores from WMI.
        /// </summary>
        /// <returns></returns>
        public int GetCpuCoreCount() => 
            int.Parse(GetWmiProcessorManagementObject()["NumberOfCores"].ToString());

        /// <summary>
        /// Get the number of threads.
        ///
        /// It should be worth noting this is the "logical" processor count on windows.
        /// </summary>
        /// <returns></returns>
        public int GetCpuThreadCount() =>
            int.Parse(GetWmiProcessorManagementObject()["NumberOfLogicalProcessors"].ToString());

        /// <summary>
        /// Get L2 Cache Size in Kilobytes.
        ///
        /// Read from WMI to get the size of the cache.
        /// </summary>
        /// <returns></returns>
        public object GetCpuCacheSize() =>
            UInt32.Parse(GetWmiProcessorManagementObject()["L2CacheSize"].ToString());

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
            long.Parse(GetWmiOperatingSystemManagementObject()["FreePhysicalMemory"].ToString());

        /// <summary>
        /// Get the total amount of physical memory in bytes.
        /// </summary>
        /// <returns></returns>
        public long GetPhysicalMemoryTotal() =>
            long.Parse(GetWmiOperatingSystemManagementObject()["TotalVisibleMemorySize"].ToString());

        /// <summary>
        /// Get the amount of virtual memory used in bytes.
        /// </summary>
        /// <returns></returns>
        public double GetVirtualMemoryUsed() =>
            GetVirtualMemoryTotal() - GetVirtualMemoryFree();

        /// <summary>
        /// Get the amount of virtual memory free in bytes.
        /// </summary>
        /// <returns></returns>
        public double GetVirtualMemoryFree() =>
            double.Parse(GetWmiOperatingSystemManagementObject()["FreeVirtualMemory"].ToString());

        /// <summary>
        /// Get the total amount of virtual memory in bytes.
        /// </summary>
        /// <returns></returns>
        public double GetVirtualMemoryTotal() =>
            double.Parse(GetWmiOperatingSystemManagementObject()["TotalVirtualMemorySize"].ToString());

        /// <summary>
        ///  Return if the system is 64 bits.
        /// </summary>
        /// <returns></returns>
        public bool Is64Bits() =>
            Environment.Is64BitOperatingSystem;


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
        /// Get information about the CPU in the form of a dictionary.
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

            // Get Virtual Memory
            Dictionary<string, object> virtualMemoryObjects = new Dictionary<string, object>()
            {
                {"Used", GetVirtualMemoryUsed()},
                {"Free", GetVirtualMemoryFree()},
                {"Total", GetVirtualMemoryTotal()}
            };

            // Returned the compiled object.
            return new Dictionary<string, object>()
            {
                {"Physical", physicalMemoryObjects},
                {"Virtual", virtualMemoryObjects}
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

        public void PurgeCachedWmiInformation()
        {
            _cachedOperatingSystemManagementObject = null;
            _cachedProccessorManagementObject = null;
        }

        public ManagementObject GetWmiOperatingSystemManagementObject(bool clearCache = false)
        {
            if (clearCache || _cachedOperatingSystemManagementObject == null)
                _cachedOperatingSystemManagementObject = new ManagementObject("SELECT * FROM Win32_OperatingSystem");

            return _cachedOperatingSystemManagementObject;
        }

        public ManagementObject GetWmiProcessorManagementObject(bool clearCache = false)
        {
            if (clearCache || _cachedProccessorManagementObject == null)
                _cachedProccessorManagementObject = new ManagementObject("SELECT * FROM Win32_Proccessor");

            return _cachedProccessorManagementObject;
        }
    }
}