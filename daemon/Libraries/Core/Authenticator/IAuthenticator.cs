using System.Threading.Tasks;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public interface IAuthenticator
    {
        Task<bool> Authenticate(string username, string password);
    }
}