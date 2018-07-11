using System;
using System.Data;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Core.Identity
{
    public class IdentityProvider : IIdentityProvider
    {
        private readonly IDbConnection _db;

        public IdentityProvider (IDbConnection db)
        {
            _db = db;
        }

        public Guid GetGuid()
        {
            var identityKey = _db.Single<Configuration>(x => x.Key == ConfigKeys.SystemIdentity);

            if (Guid.TryParse(identityKey.Value, out var result))
                return result;

            throw new EInternalError();
        }

        public string GetFQDN()
        {
            var id = GetGuid();
            return $"{id}.instances.spectero.io";
        }
    }
}