using System.Collections.Generic;

namespace Spectero.daemon.Libraries.Services
{
    public interface IService
    {
        void Start(IEnumerable<IServiceConfig> serviceConfig = null);
        void ReStart(IEnumerable<IServiceConfig> serviceConfig = null);
        void Stop();
        void Reload(IEnumerable<IServiceConfig> serviceConfig = null);
        void LogState(string caller);
        ServiceState GetState();
        IEnumerable<IServiceConfig> GetConfig();
        void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false);
    }
}