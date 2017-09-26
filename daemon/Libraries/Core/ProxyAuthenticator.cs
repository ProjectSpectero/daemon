using System;
using System.Collections.Generic;
using System.Text;
using Spectero.daemon.Libraries.Errors;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Core
{
    public class ProxyAuthenticator : IAuthenticator
    {
        public static bool Verify(string username, string password,
            string mode, Dictionary<String, Object> args)
        {
            return true;
        }

        public static bool Verify(HeaderCollection headers, Uri uri, string mode)
        {
            var authHttpHeaders = headers.GetHeaders("Proxy-Authorization");
            
            if (authHttpHeaders.Count != 1)
                throw new EAuthenticationFailed();
            
            HttpHeader authHeader = authHttpHeaders[0];
            
            if (authHeader.Value.Length <= 5)
                throw new EAuthenticationFailed();

            byte[] data = Convert.FromBase64String(authHeader.Value.Substring("Basic ".Length).Trim());
            string authString = Encoding.UTF8.GetString(data);
            string[] elements = authString.Split(':');
            
            if (elements.Length != 2)
                throw new EAuthenticationFailed();
            
            string username = elements[0];
            string password = elements[1];

            return Verify(username, password, mode, null);
        }
    }
}