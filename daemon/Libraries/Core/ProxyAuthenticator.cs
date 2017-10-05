using System;
using System.Collections.Generic;
using System.Linq;
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
            return username.Equals("a") && password.Equals("b");
        }

        public static bool Verify(HeaderCollection headers, Uri uri, string mode)
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
                    throw new EAuthenticationFailed();
            
                string username = elements[0];
                string password = elements[1];

                return Verify(username, password, mode, null);
            }
            else
                return false;
        }
    }
}