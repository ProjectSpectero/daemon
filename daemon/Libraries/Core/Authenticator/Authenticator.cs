using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public class Authenticator : IAuthenticator
    {
        private readonly AppConfig _appConfig;
        private readonly IMemoryCache _cache;
        private readonly IDbConnection _db;
        private readonly ILogger<ServiceManager> _logger;

        public Authenticator(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IMemoryCache cache)
        {
            _logger = logger;
            _appConfig = appConfig.CurrentValue;
            _db = db;
            _cache = cache;
        }

        public async Task<User> Authenticate(string username, string password, User.Actions action)
        {
            _logger.LogDebug("UPA: Attempting to verify auth for " + username);

            var user = _cache.Get<User>(Utility.GenerateCacheKey(username));

            if (user == null)
            {
                _logger.LogDebug("UPA: Cache-miss for username -> " + username + ", doing SQL lookup.");
                var dbQuery = await _db.SelectAsync<User>(x => x.AuthKey == username);
                user = dbQuery.FirstOrDefault();
                if (user != null)
                    _cache.Set(Utility.GenerateCacheKey(username), user, TimeSpan.FromMinutes(_appConfig.AuthCacheMinutes));
            }

            if (user != null)
            {
                _logger.LogDebug("UPA: User " + username + " was found, executing auth flow.");
                var passwordVerified = BCrypt.Net.BCrypt.Verify(password, user.Password);
                _logger.LogDebug("UPA: Password verification -> " + passwordVerified);
                if (passwordVerified && user.Can(action))
                    return user;
                _logger.LogDebug("UPA: User can not perform " + action.ToString());
            }
            _logger.LogDebug("UPA: Couldn't find an user named " + username);
            return null;
        }

        public async Task<bool> AuthenticateHttpProxy(string username, string password)
        {
            return await Authenticate(username, password, User.Actions.ConnectToHttpProxy) != null;
        }
    }
}