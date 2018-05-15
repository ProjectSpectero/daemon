using System;
using System.Collections.Generic;

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
        object GetCpuCacheSize();

        // Memory
        long GetPhysicalMemoryUsed();
        long GetPhysicalMemoryFree();
        long GetPhysicalMemoryTotal();

        void PurgeCachedInformation();
    }
}