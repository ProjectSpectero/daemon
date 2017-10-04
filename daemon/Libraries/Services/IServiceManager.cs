namespace Spectero.daemon.Libraries.Services
{
    public interface IServiceManager
    {
        bool Process(string name, string action);
    }
}