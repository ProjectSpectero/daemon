using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPProxy : IService
    {
        private HTTPConfig _proxyConfig;
        private readonly ProxyServer _proxyServer = new ProxyServer();
        private AppConfig _appConfig;

        public HTTPProxy(AppConfig appConfig)
        {
            _appConfig = appConfig;
        }

        public void Start(IServiceConfig serviceConfig)
        {
            this._proxyConfig = (HTTPConfig) serviceConfig;
            
            
            //Loop through and listen on all defined IP <-> port pairs
            foreach (var listener in _proxyConfig.listeners)
            {
                _proxyServer.AddEndPoint(new ExplicitProxyEndPoint(listener.Key, listener.Value, false));
            }
            
            _proxyServer.Start();
        }

        public async Task OnRequest(object sender, SessionEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.WebSession.Request.Url);
            
            var requestHeaders = eventArgs.WebSession.Request.RequestHeaders;
            var requestMethod = eventArgs.WebSession.Request.Method.ToUpper();
            var requestUri = eventArgs.WebSession.Request.RequestUri;

            if (! ProxyAuthenticator.Verify(requestHeaders, requestUri, null)) // TODO: Add mode support
                await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri, requestUri.ToString()));

            foreach (var blockedUri in _proxyConfig.bannedDomains)
            {
                if (requestUri.AbsoluteUri.Contains(blockedUri))
                    await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri, requestUri.ToString()));
            }
            
            
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public Dictionary<String, String> getStatistics ()
        {
            throw new NotImplementedException();
        }
    }
}
