using System;

namespace Spectero.daemon.Libraries.Core.Identity
{
    public class IdentityProvider : IIdentityProvider
    {
        private Guid identifier;

        public Guid GetGuid()
        {
            return identifier;
        }
    }
}