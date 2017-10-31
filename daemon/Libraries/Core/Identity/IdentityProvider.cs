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
            // Todo: fetch identity from the Spectero cloud
            return Guid.NewGuid();
        }
    }
}