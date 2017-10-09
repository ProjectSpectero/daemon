using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Services;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Core
{
    public class Authenticator : IAuthenticator
    {
        private ILogger<ServiceManager> _logger;
        private IDbConnection _db;
        private AppConfig _appConfig;

        public Authenticator(AppConfig appConfig, ILogger<ServiceManager> logger, IDbConnection db)
        {
            _logger = logger;
            _appConfig = appConfig;
            _db = db;
        }

        public bool Authenticate(string username, string password)
        {
            return username.Equals("a") && password.Equals("b");
        }
        
        // TODO: Add mode support
        public bool Authenticate (HeaderCollection headers, Uri uri, string mode)
        {
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

                return Authenticate(username, password);

            }
            else
                return false;
        }
        
        
       
    }
}