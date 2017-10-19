namespace Spectero.daemon.Libraries.Services
{
    public interface IService
    {
        void Start(IServiceConfig serviceConfig);
        void ReStart(IServiceConfig serviceConfig);
        void Stop();
        void Reload(IServiceConfig serviceConfig);

        void LogState(string caller);
        ServiceState GetState();
    }
}