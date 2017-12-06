using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Statistics;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    /*
     *  TODO: c. Service restart :V
     *  TODO: d. Mode support
     */

    public class HTTPProxy : IService
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly IEnumerable<IPAddress> _localAddresses;
        private readonly ILogger<ServiceManager> _logger;
        private readonly ProxyServer _proxyServer = new ProxyServer();
        private readonly IStatistician _statistician;
        private HTTPConfig _proxyConfig;

        private ServiceState State = ServiceState.Halted;

        public HTTPProxy(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IEnumerable<IPAddress> localAddresses,
            IStatistician statistician)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _localAddresses = localAddresses;
            _statistician = statistician;
        }

        public HTTPProxy()
        {
        }

        public void Start(IServiceConfig serviceConfig = null)
        {
            LogState("Start");
            if (State == ServiceState.Halted)
            {
                if (serviceConfig != null)
                    _proxyConfig = (HTTPConfig) serviceConfig;

                //Loop through and listen on all defined IP <-> port pairs
                foreach (var listener in _proxyConfig.listeners)
                {
                    var endpoint = new ExplicitProxyEndPoint(IPAddress.Parse(listener.Item1), listener.Item2,
                        false);
                    endpoint.ExcludedHttpsHostNameRegex = new List<string> { ".*" };
                    _proxyServer.AddEndPoint(endpoint);
                    _logger.LogDebug("SS: Now listening on " + listener.Item1 + ":" + listener.Item2);
                }

                _proxyServer.ProxyRealm = "Spectero";
                _proxyServer.AuthenticateUserFunc += _authenticator.Authenticate;
                _proxyServer.BeforeRequest += OnRequest;
                _proxyServer.BeforeResponse += OnResponse;
                _proxyServer.TunnelConnectRequest += OnTunnelConnectRequest;
                _proxyServer.TunnelConnectResponse += OnTunnelConnectResponse;
                _proxyServer.ExceptionFunc = exception => _logger.LogDebug(exception.Message);


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

        public void Reload(IServiceConfig serviceConfig)
        {
            _proxyConfig = (HTTPConfig) serviceConfig;
        }

        public void LogState(string caller)
        {
            _logger.LogDebug("[" + GetType().Name + "][" + caller + "] Current state is " + State);
        }

        public ServiceState GetState()
        {
            return State;
        }

        private void CheckProxyMode(string host, ref string failReason)
        {
            if (failReason.IsNullOrEmpty() && _proxyConfig.proxyMode == HTTPProxyModes.ExclusiveAllow)
            {
                if (_proxyConfig.allowedDomains != null)
                {
                    var matchFound = false;
                    foreach (var allowedHost in _proxyConfig.allowedDomains)
                        if (host.Equals(allowedHost))
                        {
                            matchFound = true;
                            break;
                        }
                    if (!matchFound)
                        failReason = BlockedReasons.ExclusiveAllow;
                }
                else
                {
                    _logger.LogError(
                        "ESO: Proxy is set to start in exclusive-allow mode, but list of domains is empty. This will mean that ALL traffic will be dropped.");
                    failReason = BlockedReasons.ExclusiveAllow;
                }
            }
        }

        private void CheckBannedDomain(string host, ref string failReason)
        {
            if (failReason.IsNullOrEmpty() && _proxyConfig.bannedDomains != null)
                foreach (var blockedUri in _proxyConfig.bannedDomains)
                {
                    if (!host.Equals(blockedUri)) continue;
                    _logger.LogDebug("ESO: Blocked host " + host + " found.");
                    failReason = BlockedReasons.BlockedUri;
                    break;
                }
        }

        private void CheckLanProtection(string host, ref string failReason)
        {           
            if (failReason.IsNullOrEmpty() && _appConfig.LocalSubnetBanEnabled)
            {
                var hostAddresses = Dns.GetHostAddresses(host);
                if (hostAddresses.Length == 0) return;

                foreach (var network in _localNetworks)
                foreach (var address in hostAddresses)
                {
                    if (!IPNetwork.Contains(network, address)) continue;
                    _logger.LogDebug("ESO: Found access attempt to LAN (" + address + " is in " + network + ")");
                    failReason = BlockedReasons.LanProtection;
                    break;
                }
            }
        }



        private async Task OnTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs eventArgs)
        {
            await OnRequest(sender, eventArgs as SessionEventArgs);
        }

        private async Task OnRequest(object sender, SessionEventArgs eventArgs)
        {
            _logger.LogDebug("ESO: Processing request to " + eventArgs.WebSession.Request.Url);

            string failReason = null;

            var request = eventArgs.WebSession.Request;
            var requestUri = request.RequestUri;
            var host = requestUri.Host;

            await _statistician.Update<HTTPProxy>(CalculateObjectSize(request),
                DataFlowDirections.Out);

            CheckProxyMode(host, ref failReason);
            CheckBannedDomain(host, ref failReason);
            CheckLanProtection(host, ref failReason);

            SetUpstreamAddress(ref eventArgs);


            if (failReason != null)
                await eventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri, failReason,
                    Uri.EscapeDataString(requestUri.ToString())));
        }

        private async Task OnTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs eventArgs)
        {
            await OnResponse(sender, eventArgs as SessionEventArgs);
        }

        private async Task OnResponse(object sender, SessionEventArgs eventArgs)
        {
            await _statistician.Update<HTTPProxy>(CalculateObjectSize(eventArgs.WebSession.Response),
                DataFlowDirections.In);
        }



        private void SetUpstreamAddress(ref SessionEventArgs eventArgs)
        {
            var requestedUpstream =
                Utility.ExtractHeader(eventArgs.WebSession.Request.Headers, "X-SPECTERO-UPSTREAM-IP")
                    .FirstOrDefault();

            if (requestedUpstream != null)
            {
                // Found a request for a specific upstream
                IPAddress requestedAddress;
                if (IPAddress.TryParse(requestedUpstream.Value, out requestedAddress))
                {
                    _logger.LogDebug("ES: Proxy request received with upstream request of " + requestedAddress);
                    if (_localAddresses.Contains(requestedAddress))
                    {
                        _logger.LogDebug("ES: Requested address is valid (" + requestedAddress + ")");
                        eventArgs.WebSession.UpStreamEndPoint = new IPEndPoint(requestedAddress, 0);
                    }
                    else
                        _logger.LogDebug(
                            "ES: Requested address is NOT valid for this system (" + requestedAddress + ")");
                }
                else
                    _logger.LogDebug("ES: Invalid X-SPECTERO-UPSTREAM-IP header.");
            }
            else
            {
                // Default behavior is to set the same endpoint the request came in via as the outgoing address in multi-ip deployment scenarios
                // This does not trigger for local (127/8, ::1, 0.0.0.0 and such addresses either)
                
                var endpoint = eventArgs.LocalEndPoint;
                
                if (Utility.CheckIPFilter(endpoint.IpAddress, Utility.IPComparisonReasons.FOR_PROXY_OUTGOING) && _appConfig.RespectEndpointToOutgoingMapping)
                {
                    _logger.LogDebug("ES: No header upstream was requested, using endpoint default of " + endpoint.IpAddress + " as it was determined valid and RespectEndpointToOutgoingMapping is enabled.");
                    eventArgs.WebSession.UpStreamEndPoint = new IPEndPoint(endpoint.IpAddress, 0);
                }
                
            }
                
        }

        private long CalculateObjectSize(Request request)
        {
            long ret = 0;
            if (request.ContentLength > 0)
                ret += request.ContentLength;

            ret += request.HeaderText.ToAsciiBytes().Length;
            ret += request.RequestUri.ToString().ToAsciiBytes().Length;

            return ret;
        }

        private long CalculateObjectSize(Response response)
        {
            long ret = 0;
            if (response.ContentLength > 0)
                ret += response.ContentLength;
            else
                ret = 64; // Assume a response is at least 64 bytes if a non 2-xx header was encountered

            return ret;
        }

        public IServiceConfig GetConfig()
        {
            return _proxyConfig;
        }

        public void SetConfig(IServiceConfig config, bool restartNeeded = false)
        {
            _proxyConfig = (HTTPConfig) config;
        }
    }
}