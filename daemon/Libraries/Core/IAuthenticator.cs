using System;
using Titanium.Web.Proxy.Http;

namespace Spectero.daemon.Libraries.Core
{
    public interface IAuthenticator
    {
        bool Authenticate(string username, string password);
        bool Authenticate(HeaderCollection headers, Uri uri, string mode);
    }
}