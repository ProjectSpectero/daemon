using System;

namespace Spectero.daemon.Libraries.Core.Identity
{
    public interface IIdentityProvider
    {
        Guid GetGuid();
        string GetFQDN();
    }
}