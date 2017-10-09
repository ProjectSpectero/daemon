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

        public async Task<bool> Authenticate(string username, string password)
        {
            _logger.LogDebug("UPA: Attempting to auth using u -> " + username + ", p -> " + password);
            
            var dbQuery = await _db.SelectAsync<User>( x => x.AuthKey == username );
            var user = dbQuery.FirstOrDefault();

            if (user == null)
            {
                _logger.LogDebug("UPA: Couldn't find an user named " + username);
                return false;
            }
               
            return Argon2.Verify(user.Password, password); // Hash first, pw second
        }
        
        // TODO: Add mode support
        public async Task<bool> Authenticate (HeaderCollection headers, Uri uri, string mode)
        {
            _logger.LogDebug("HUMA: Processing request to " + uri);
            var authHeader = ((IEnumerable<HttpHeader>) headers.ToArray<HttpHeader> ())
                .FirstOrDefault<HttpHeader>((Func<HttpHeader, bool>) 
                    (
                        t => t.Name == "Proxy-Authorization"
                    )
                );

            if (authHeader == null)
                return false;
           
            if (authHeader.Value.StartsWith("Basic"))
            {
                byte[] data = Convert.FromBase64String(authHeader.Value.Substring("Basic ".Length).Trim());
                string authString = Encoding.UTF8.GetString(data);
                string[] elements = authString.Split(':');

                if (elements.Length != 2)
                    return false;
            
                string username = elements[0];
                string password = elements[1];

                return await Authenticate(username, password);

            }
            else
                return false;
        }
        
        
       
    }
}