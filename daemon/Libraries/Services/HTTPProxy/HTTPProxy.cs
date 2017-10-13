using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack.Templates;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Statistics;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    /*
     *  TODO: b. Statistics
     *  TODO: c. Service restart :V
     *  TODO: d. Mode support
     */

    public class HTTPProxy : IService
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly ILogger<ServiceManager> _logger;
        private readonly ProxyServer _proxyServer = new ProxyServer();
        private readonly IStatistician _statistician;
        private HTTPConfig _proxyConfig;

        private ServiceState State = ServiceState.Halted;

        public HTTPProxy(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IStatistician statistician)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _statistician = statistician;
        }

        public HTTPProxy()
        {
        }

        public void Start(IServiceConfig serviceConfig)
        {
            LogState("Start");
            if (State == ServiceState.Halted)
            {
                _proxyConfig = (HTTPConfig) serviceConfig;

                //Loop through and listen on all defined IP <-> port pairs
                foreach (var listener in _proxyConfig.listeners)
                {
                    _proxyServer.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Parse(listener.Item1), listener.Item2, false));
                    _logger.LogDebug("SS: Now listening on " + listener.Item1 + ":" + listener.Item2);
                }

                _proxyServer.AuthenticateUserFunc += _authenticator.Authenticate;
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

        public void LogState(string caller)
        {
            _logger.LogDebug("[" + GetType().Name + "][" + caller + "] Current state is " + State);
        }

        private async Task OnRequest(object sender, SessionEventArgs eventArgs)
        {
            _logger.LogDebug("SEO: Processing request to " + eventArgs.WebSession.Request.Url);

            string failReason = null;

            var request = eventArgs.WebSession.Request;
            var requestUri = request.RequestUri;

            // TODO: Request size calculation is broken, fix is important for tracking uploads.

            await _statistician.Update<HTTPProxy>(eventArgs.WebSession.Request.ContentLength,
                DataFlowDirections.Out);

            var host = requestUri.Host;
            var hostAddresses = Dns.GetHostAddresses(host);

            if (_appConfig.LocalSubnetBanEnabled && hostAddresses.Length >= 0)
                foreach (var network in _localNetworks)
                foreach (var address in hostAddresses)
                {
                    if (!IPNetwork.Contains(network, address)) continue;
                    _logger.LogDebug("SEO: Found access attempt to LAN (" + address + " is in " + network + ")");
                    failReason = BlockedReasons.LanProtection;
                    break;
                }

            if (failReason == null)
                foreach (var blockedUri in _proxyConfig.bannedDomains)
                {
                    if (!requestUri.AbsoluteUri.Contains(blockedUri)) continue;
                    _logger.LogDebug("SEO: Blocked URI " + blockedUri + " found in " + requestUri);
                    failReason = BlockedReasons.BlockedUri;
                    break;
                }

            if (failReason != null)
                await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri, failReason,
                    Uri.EscapeDataString(requestUri.ToString())));
        }

        private async Task OnResponse(object sender, SessionEventArgs eventArgs)
        {
            await _statistician.Update<HTTPProxy>(eventArgs.WebSession.Response.ContentLength,
                DataFlowDirections.In);
        }

        private double CalculateEventSize(Request request)
        {
            return Encoding.Default.GetBytes(request.AsString()).Length;
        }
    }
}