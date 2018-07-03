using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Services
{
    public interface IServiceConfig
    {
        Task<string> GetStringConfig();
    }
}