using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Models;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public class Authenticator : IAuthenticator
    {
        private readonly ILogger<ServiceManager> _logger;
        private readonly IDbConnection _db;
        private readonly AppConfig _appConfig;
        private readonly IMemoryCache _cache;
        
        public Authenticator(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IMemoryCache cache)
        {
            _logger = logger;
            _appConfig = appConfig.CurrentValue;
            _db = db;
            _cache = cache;
        }

        public async Task<bool> Authenticate (string username, string password)
        {
            _logger.LogDebug("UPA: Attempting to auth using u -> " + username + ", p -> " + password);

            User user = _cache.Get<User>(GenerateCacheKey(username));

            if (user == null)
            {
                _logger.LogDebug("UPA: Cache-miss for username -> " + username + ", doing SQLite lookup.");
                var dbQuery = await _db.SelectAsync<User>( x => x.AuthKey == username );
                user = dbQuery.FirstOrDefault();
                if (user != null)
                    _cache.Set(GenerateCacheKey(username), user, TimeSpan.FromMinutes(_appConfig.AuthCacheMinutes));
            }

            if (user != null)
            {
                var ret = Argon2.Verify(user.Password, password);
                _logger.LogDebug("UPA: Argon2 said " + ret);
                return ret;
            }
                
            else
            {
                _logger.LogDebug("UPA: Couldn't find an user named " + username);
                return false;
            }         
        }
       
        private string GenerateCacheKey(string username)
        {
            return "auth.user." + username;
        }
        
        
       
    }
}