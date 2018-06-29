namespace Spectero.daemon.Libraries.Core.LifetimeHandler
{
    public interface ILifetimeHandler
    {
        void OnStarted();
        void OnStopping();
        void OnStopped();
    }
}