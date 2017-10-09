using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    public class HTTPProxy : IService
    {
        private HTTPConfig _proxyConfig;
        private readonly ProxyServer _proxyServer = new ProxyServer();
        private readonly AppConfig _appConfig;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IDbConnection _db;
        private readonly IAuthenticator _authenticator;
        
        private readonly IDictionary<Guid, string> _requestBodyHistory 
            = new ConcurrentDictionary<Guid, string>();
        
        private ServiceState State = ServiceState.Halted;

        public HTTPProxy(AppConfig appConfig, ILogger<ServiceManager> logger, IDbConnection db, IAuthenticator authenticator)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
        }

        public HTTPProxy()
        {
            
        }

        public void Start(IServiceConfig serviceConfig)
        {
            LogState("Start");
            if (State == ServiceState.Halted)
            {
                this._proxyConfig = (HTTPConfig) serviceConfig;
            
                //Loop through and listen on all defined IP <-> port pairs
                foreach (var listener in _proxyConfig.listeners)
                {
                    _proxyServer.AddEndPoint(new ExplicitProxyEndPoint(listener.Key, listener.Value, false));
                    _logger.LogDebug("Now listening on " + listener.Key.ToString() + ":" + listener.Value.ToString());
                }

                _proxyServer.BeforeRequest += OnRequest;
                _proxyServer.BeforeResponse += OnResponse;
            
                _proxyServer.Start();
                State = ServiceState.Running;
            }
            LogState("Start");
        }
        
        public void Stop()
        {
            // TODO: fix stop, causes a crash atm.
            LogState("Stop");
            if (State == ServiceState.Running)
            {
                _proxyServer.Stop();
                State = ServiceState.Halted;
            }
            LogState("Stop");
        }

        public void ReStart(IServiceConfig serviceConfig = null)
        {
            LogState("ReStart");
            if (State == ServiceState.Running)
            {
                Stop();
                Start(serviceConfig ?? _proxyConfig);
            }
            LogState("ReStart");
        }
       
        public async Task OnRequest(object sender, SessionEventArgs eventArgs)
        {
            _logger.LogDebug("Processing request to " + eventArgs.WebSession.Request.Url);

            var requestHeaders = eventArgs.WebSession.Request.RequestHeaders;
            var requestMethod = eventArgs.WebSession.Request.Method.ToUpper();
            var requestUri = eventArgs.WebSession.Request.RequestUri;

            if (! _authenticator.Authenticate(requestHeaders, requestUri, null))
                await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri,
                    Uri.EscapeDataString(requestUri.ToString())));

            foreach (var blockedUri in _proxyConfig.bannedDomains)
            {
                if (requestUri.AbsoluteUri.Contains(blockedUri))
                    await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri, requestUri.ToString()));
            }
        }

        public async Task OnResponse(object sender, SessionEventArgs eventArgs)
        {
            //read response headers
            var responseHeaders = eventArgs.WebSession.Response.ResponseHeaders;

            //if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            if (eventArgs.WebSession.Request.Method == "GET" || eventArgs.WebSession.Request.Method == "POST")
            {
                if (eventArgs.WebSession.Response.ResponseStatusCode == 200)
                {
                    if (eventArgs.WebSession.Response.ContentType!=null && eventArgs.WebSession.Response.ContentType.Trim().ToLower().Contains("text/html"))
                    {
                        byte[] bodyBytes = await eventArgs.GetResponseBody();
                        await eventArgs.SetResponseBody(bodyBytes);

                        string body = await eventArgs.GetResponseBodyAsString();
                        await eventArgs.SetResponseBodyString(body);
                    }
                }
            }
    
            //access request body/request headers etc by looking up using requestId
            if(_requestBodyHistory.ContainsKey(eventArgs.Id))
            {
                var requestBody = _requestBodyHistory[eventArgs.Id];
            }
        }
        
        public void LogState(string caller)
        {
            _logger.LogDebug("[" + GetType().Name +"][" + caller + "] Current state is " + State);
        }
    }
}
