namespace Spectero.daemon.Libraries.Services
{
    public interface IService
    {
        void Start(IServiceConfig serviceConfig = null);
        void ReStart(IServiceConfig serviceConfig = null);
        void Stop();
        void Reload(IServiceConfig serviceConfig = null);
        void LogState(string caller);
        ServiceState GetState();
        IServiceConfig GetConfig();
        void SetConfig(IServiceConfig config);
    }
}