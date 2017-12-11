using System.Threading.Tasks;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public interface IAuthenticator
    {
        Task<User> Authenticate(string username, string password, User.Action action);
        Task<bool> AuthenticateHttpProxy(string username, string password);
    }
}