using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.Config
{
    public interface IServiceConfigManager
    {
        IServiceConfig Generate<T>() where T : new();
    }
}