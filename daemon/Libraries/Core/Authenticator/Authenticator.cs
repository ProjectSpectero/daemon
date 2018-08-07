/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
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
using Titanium.Web.Proxy.EventArguments;

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

        public async Task<User> Authenticate(string username, string password, User.Action action)
        {
            _logger.LogDebug($"UPA: Attempting to verify auth for {username}");

            var user = _cache.Get<User>(AuthUtils.GetCachedUserKey(username));

            if (user == null)
            {
                _logger.LogDebug($"UPA: Cache-miss for username -> {username}, doing SQL lookup.");
                var dbQuery = await _db.SelectAsync<User>(x => x.AuthKey == username);
                user = dbQuery.FirstOrDefault();
                
                // Cache user object so we don't hit SQLite too much. This kills direct DB manipulations for `AuthCacheMinutes`, be aware!
                // All the local user alter routines are equipped with additional support for clearing cached data, so internally, this is fine.
                if (user != null)
                    _cache.Set(AuthUtils.GetCachedUserKey(username), user, TimeSpan.FromMinutes(_appConfig.AuthCacheMinutes));

            }

            if (user != null)
            {
                _logger.LogDebug($"UPA: User {username} was found, executing auth flow.");

                // This verification is a reason for performance problems on rapid fire services like the proxy,
                // where requests are authenticated on every request
                var passwordVerified = false;

                if (_appConfig.InMemoryAuth && _cache.TryGetValue(AuthUtils.GetCachedUserPasswordKey(username), out var cachedPassword))
                {
                    _logger.LogDebug("UPA: In-memory password cache used.");
                    var cachedString = (string) cachedPassword;

                    if (cachedString.Equals(password))
                        passwordVerified = true;
                }
                else
                {
                    passwordVerified = BCrypt.Net.BCrypt.Verify(password, user.Password);

                    if (passwordVerified && _appConfig.InMemoryAuth)
                    {
                        // Only put the user's password in the cache if In-memory auth data caching is enabled and PW was verified
                        _cache.Set(AuthUtils.GetCachedUserPasswordKey(username), password, TimeSpan.FromMinutes(_appConfig.InMemoryAuthCacheMinutes));
                    }
                }
                    

                _logger.LogDebug($"UPA: Password verification -> {passwordVerified}");
                if (passwordVerified && user.Can(action))
                    return user;
                _logger.LogDebug($"UPA: User can not perform {action}");
            }
            _logger.LogDebug($"UPA: Couldn't find an user named {username} that matched the given credentials.");
            return null;
        }

        // TODO: Use the SessionEventArgsBase for improved authentication.
        public async Task<bool> AuthenticateHttpProxy(SessionEventArgsBase eventArgsBase, string username, string password)
        {
            return await Authenticate(username, password, User.Action.ConnectToHTTPProxy) != null;
        }
    }
}