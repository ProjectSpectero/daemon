using System;

namespace Spectero.daemon.Libraries.Core.Identity
{
    public class IdentityProvider : IIdentityProvider
    {
        private Guid identifier;

        public IdentityProvider ()
        {
            
        }

        public Guid GetGuid()
        {
            return Guid.NewGuid();
        }
    }
}