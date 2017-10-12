using System;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Http;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public interface IAuthenticator
    {
        Task<bool> Authenticate(string username, string password);
    }
}