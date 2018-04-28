using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.APM
{
    /// <summary>
    /// Interface of System Environemnts
    ///
    /// Each environment should have these functions for clarity.
    /// </summary>
    public interface ISystemEnvironment
    {
        // CPU
        string GetCpuName();
        int GetCpuCoreCount();
        int GetCpuThreadCount();
        Object GetCpuCacheSize();

        // Memory
        long GetPhysicalMemoryUsed();
        long GetPhysicalMemoryFree();
        long GetPhysicalMemoryTotal();

        // Arch
        bool Is64Bits();

        // Dictionary Getters
        Dictionary<string, object> GetAllDetails();
        Dictionary<string, object> GetCpuDetails();
        Dictionary<string, object> GetMemoryDetails();
        Dictionary<string, object> GetEnvironmentDetails();
    }
}
