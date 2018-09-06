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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Statistics;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Spectero.daemon.Libraries.Services.HTTPProxy
{
    // ReSharper disable once InconsistentNaming
    public class HTTPProxy : BaseService
    {
        private readonly IStatistician _statistician;
        private new readonly ILogger<HTTPProxy> _logger;
     

        private readonly List<string> _cacheKeys;
        private readonly Uri _blockedRedirectUri;
        private readonly ProxyServer _proxyServer;

        // Variables which get modified while executing (the cache one is more of a failsafe).
        private IMemoryCache _cache;
        private HTTPConfig _proxyConfig;
        private ServiceState _state = ServiceState.Halted;
        

        public HTTPProxy(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<HTTPProxy>>();
            _statistician = serviceProvider.GetRequiredService<IStatistician>();
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();

            // Constructor param disables asking it to import a local root cert
            _proxyServer = new ProxyServer(false);
            
            _cacheKeys = new List<string>();
            _blockedRedirectUri = new Uri(_appConfig.BlockedRedirectUri);
        }

        public HTTPProxy()
        {
            
        }

        public override void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            LogState("Start");

            if (_state == ServiceState.Halted)
            {
                if (serviceConfig != null)
                    SetConfig(serviceConfig);
                

                // Stop proxy server if it's somehow internally running due to a mismatched state (due to errors on a previous startup)
                if (_proxyServer.ProxyRunning)
                {
                    _logger.LogWarning("HTTPProxy: Engine's state and ours do not match. This is not supposed to happen! Attempting to fix...");
                    _proxyServer.Stop();
                }

                // Remove all old endpoints, if they exist, prior to startup.
                // Everything else will be reset anyway.
                var existingEndPoints = _proxyServer.ProxyEndPoints.ToArray();
                foreach (var endPoint in existingEndPoints)
                {
                    _proxyServer.RemoveEndPoint(endPoint);
                }
                   
                //Loop through and listen on all defined IP <-> port pairs
                foreach (var listener in _proxyConfig.listeners)
                {
                    var ipAddress = IPAddress.Parse(listener.Item1);
                    var port = listener.Item2;

                    // Yep, there's a chance of an exception. We want it to happen if config is bogus, no handling.
                    _portRegistry.Allocate(ipAddress, port, TransportProtocol.TCP, this);

                    var endpoint = new ExplicitProxyEndPoint(ipAddress, port, false);

                    endpoint.BeforeTunnelConnectRequest += OnTunnelConnectRequest;
                    endpoint.BeforeTunnelConnectResponse += OnTunnelConnectResponse;

                    _proxyServer.AddEndPoint(endpoint);
                    _logger.LogDebug($"HTTPProxy: Now listening on {listener.Item1}:{listener.Item2}");
                }

                _proxyServer.ProxyAuthenticationRealm = "Spectero";
                _proxyServer.ProxyBasicAuthenticateFunc += _authenticator.AuthenticateHttpProxy;
                _proxyServer.BeforeRequest += OnRequest;
                _proxyServer.BeforeResponse += OnResponse;
                _proxyServer.ExceptionFunc = HandleInternalProxyError;


                _proxyServer.Start();
                _state = ServiceState.Running;
                _logger.LogInformation($"HTTPProxy: now listening on {_proxyConfig.listeners.Count} endpoint(s).");
            }
            LogState("Start");
        }

        private void HandleInternalProxyError(Exception exception)
        {
            if (_appConfig.LogCommonProxyEngineErrors)
                _logger.LogWarning(exception, "Internal error on the proxy engine: ");
            else
                _logger.LogDebug(exception, "Internal Error on the Proxy Engine: ");
        }

        public override void Stop()
        {
            LogState("Stop");
            if (_state == ServiceState.Running)
            {
                _proxyServer.Stop();
                _portRegistry.CleanUp(this);
                _state = ServiceState.Halted;
            }
            LogState("Stop");
        }

        public override void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            LogState("ReStart");
            if (_state == ServiceState.Running)
            {
                Stop();
                Start(serviceConfig);
            }
            LogState("ReStart");
        }

        public override void Reload(IEnumerable<IServiceConfig> serviceConfig)
        {
            SetConfig(serviceConfig);
        }

        public override void LogState(string caller)
        {
            _logger.LogDebug($"[{GetType().Name}][{caller}] Current state is {_state}");
        }

        public override ServiceState GetState()
        {
            return _state;
        }

        private void CheckProxyMode(string host, ref string failReason)
        {
            if (failReason.IsNullOrEmpty() && _proxyConfig.proxyMode == HTTPProxyModes.ExclusiveAllow)
            {
                if (_proxyConfig.allowedDomains != null)
                {
                    if (!GetFromCache(FormatCacheKey(host, "allowedDomains"), out var test))
                    {
                        // OK, this is not one of the blocked domains.
                        // Let's check if it's the blocked-descriptor URI however.
                        if (host.Equals(_blockedRedirectUri.Host))
                            return;

                        // Ok, this connection may be blocked.
                        failReason = BlockedReasons.ExclusiveAllow;

                        _logger.LogDebug($"ESO: Blocked connection attempt to {host} because it is not in the allowed domains list.");
                    }
                            
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
            if (failReason.IsNullOrEmpty() && _proxyConfig.bannedDomains != null &&
                GetFromCache(FormatCacheKey(host, "bannedDomains"), out var test))
            {
                _logger.LogDebug($"ESO: Blocked host {host} found.");
                failReason = BlockedReasons.BlockedUri;
            }
        }

        private void CheckLanProtection(string host, ref string failReason, ref string blockedAddress)
        {           
            if (failReason.IsNullOrEmpty() && _appConfig.LocalSubnetBanEnabled)
            {
                var key = FormatCacheKey(host, "lanProtection");

                if (GetFromCache(key, out var matchResult))
                {
                    var result = (Dictionary<string, string>) matchResult;
                    var matched = bool.Parse(result["matched"]);

                    if (matched)
                    {
                        _logger.LogDebug(
                            $"ESO-CACHE: Found access attempt to LAN ({result["address"]} is in {result["network"]})");
                        failReason = BlockedReasons.LanProtection;
                        blockedAddress = result["address"];
                    }
                    else
                        _logger.LogDebug($"ESO-CACHE: {host} verified, LAN protection bypassed.");
                }
                else
                {
                    // Not in cache, likely an host we're seeing for the first time.
                    var cacheTarget = new Dictionary<string, string> {{"host", host}};

                    var hostAddresses = Dns.GetHostAddresses(host);
                    if (hostAddresses.Length == 0)
                    {
                        _logger.LogInformation($"ESO: Could not resolve {host}, LAN protection bypassed.");
                        return;
                    }

                    foreach (var network in _localNetworks)
                        foreach (var address in hostAddresses)
                        {
                            if (!IPNetwork.Contains(network, address)) continue;

                            _logger.LogDebug($"ESO-LOOP: Found access attempt to LAN ({address} is in {network})");
                            failReason = BlockedReasons.LanProtection;
                            blockedAddress = address.ToString();

                            cacheTarget.Add("matched", true.ToString());
                            cacheTarget.Add("address", address.ToString());
                            cacheTarget.Add("network", network.ToString());
                            break;
                        }

                    if (failReason.IsNullOrEmpty())
                        cacheTarget.Add("matched", false.ToString());

                    // Cache the result for 5 minutes
                    AddToCache(key, cacheTarget, 5);
                }
      
            }
        }


        private async Task OnTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs eventArgs)
        {
            await HandleProxyRequest(sender, eventArgs, true);
        }

        private async Task OnRequest(object sender, SessionEventArgs sessionEventArgs)
        {
            await HandleProxyRequest(sender, sessionEventArgs, false);
        }


        private async Task HandleProxyRequest(object sender, SessionEventArgsBase eventArgs, bool isTunnel = false)
        {
            _logger.LogDebug("ESO: Processing request to " + eventArgs.WebSession.Request.Url);

            string failReason = null;
            var data = "";

            var request = eventArgs.WebSession.Request;
            var requestUri = request.RequestUri;
            var host = requestUri.Host;

            await _statistician.Update<HTTPProxy>(CalculateObjectSize(request),
                DataFlowDirections.Out);

            CheckProxyMode(host, ref failReason);
            CheckBannedDomain(host, ref failReason);

            CheckLanProtection(host, ref failReason, ref data);

            SetUpstreamAddress(ref eventArgs);

            // Can't redirect SSL requests
            if (failReason != null && eventArgs is SessionEventArgs)
            {
                var castEventArgs = (SessionEventArgs) eventArgs;
                castEventArgs.Redirect(string.Format(_appConfig.BlockedRedirectUri,
                    failReason,
                    Uri.EscapeDataString(requestUri.ToString()), data));
            }

        }

        private async Task OnTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs eventArgs)
        {
            await OnResponse(sender, eventArgs);
        }

        private async Task OnResponse(object sender, SessionEventArgsBase eventArgs)
        {
            await _statistician.Update<HTTPProxy>(CalculateObjectSize(eventArgs.WebSession.Response),
                DataFlowDirections.In);
        }



        private void SetUpstreamAddress(ref SessionEventArgsBase eventArgs)
        {
            var requestedUpstream =
                Utility.ExtractHeader(eventArgs.WebSession.Request.Headers, "X-SPECTERO-UPSTREAM-IP")
                    .FirstOrDefault();

            IPAddress requestedAddress = null;

            if (requestedUpstream != null)
            {
                // Found a request for a specific upstream
                
                if (IPAddress.TryParse(requestedUpstream.Value, out requestedAddress))
                {
                    _logger.LogDebug($"ES: Proxy request received with upstream request of {requestedAddress}");

                    // This lookup is possibly slow, TBD.
                    if (_localAddresses.Contains(requestedAddress))
                        _logger.LogDebug($"ES: Requested address is valid ({requestedAddress})");
                      
                    else
                        _logger.LogWarning(
                            $"ES: Requested address is NOT valid for this system ({requestedAddress}), silently ignored. Request originated with system default IP.");
                }
                else
                    _logger.LogWarning("ES: Invalid X-SPECTERO-UPSTREAM-IP header.");
            }
            else
            {
                // Default behavior is to set the same endpoint the request came in via as the outgoing address in multi-ip deployment scenarios
                // This does not trigger for local (127/8, ::1, 0.0.0.0 and such addresses either)
                
                var endpoint = eventArgs.LocalEndPoint;
                
                if (Utility.CheckIPFilter(endpoint.IpAddress, Utility.IPComparisonReasons.FOR_PROXY_OUTGOING) && _appConfig.RespectEndpointToOutgoingMapping)
                {
                    requestedAddress = endpoint.IpAddress;
                    _logger.LogDebug(
                        $"ES: No header upstream was requested, using endpoint default of {requestedAddress} as it was determined valid and RespectEndpointToOutgoingMapping is enabled.");                
                }

                if (requestedAddress != null)
                    eventArgs.WebSession.UpStreamEndPoint = new IPEndPoint(requestedAddress, 0);
                
            }
                
        }

        private static long CalculateObjectSize(Request request)
        {
            long ret = 0;
            if (request.ContentLength > 0)
                ret += request.ContentLength;

            ret += request.HeaderText.ToAsciiBytes().Length;
            ret += request.RequestUri.ToString().ToAsciiBytes().Length;

            return ret;
        }

        private static long CalculateObjectSize(Response response)
        {
            long ret = 0;
            if (response.ContentLength > 0)
                ret += response.ContentLength;
            else
                ret = 64; // Assume a response is at least 64 bytes if a non 2-xx header was encountered

            return ret;
        }

        public override IEnumerable<IServiceConfig> GetConfig()
        {
            return new List<IServiceConfig> { _proxyConfig };
        }

        public override void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            ClearLocalCache();

            // This service does not support "instances," i.e: the first config is the only useful one.
            _proxyConfig = (HTTPConfig) config.First();

            if (_proxyConfig.allowedDomains != null)
            {
                foreach (var domain in _proxyConfig.allowedDomains)
                {
                    var key = FormatCacheKey(domain, "allowedDomains");
                    AddToCache(key, true);
                }
            }

            if (_proxyConfig.bannedDomains != null)
            {
                foreach (var domain in _proxyConfig.bannedDomains)
                {
                    var key = FormatCacheKey(domain, "bannedDomains");
                    AddToCache(key, true);
                }
            }
          
        }

        private bool GetFromCache(string key, out object value)
        {
            CheckAndFixCacheIfNeeded();

            return _cache.TryGetValue(key, out value);
        }

        private void AddToCache (string key, object data, int expiryTimespan = 0)
        {
            CheckAndFixCacheIfNeeded();
            var cacheExpirationOptions = new MemoryCacheEntryOptions();


            if (expiryTimespan != 0)
                cacheExpirationOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryTimespan);

            cacheExpirationOptions.RegisterPostEvictionCallback(callback: (cacheKey, value, reason, state) =>
            {
                _logger.LogTrace($"Log entry {key} was evicted from the cache due to {reason}.");

                // Stop leaking cache keys, clean it up as we go.
                _cacheKeys.Remove(cacheKey as string);
            });

            _cache.Set(key, data, cacheExpirationOptions);
            _cacheKeys.Add(key);
        }

        private void RemoveFromCache(string key, bool removeKey = true)
        {
            CheckAndFixCacheIfNeeded();
            _cache.Remove(key);

            if (removeKey)
                _cacheKeys.Remove(key);
        }

        private void ClearLocalCache()
        {
            foreach (var key in _cacheKeys.ToArray())
            {
                // Why? Can't operate on list we're currently enumerating
                RemoveFromCache(key, false);
            }

            _cacheKeys.Clear();
        }

        private void CheckAndFixCacheIfNeeded()
        {
            // This is a very dirty hack
            // TODO: assess whether we actually need this after we have a few days worth of runtime data.
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                _cache.ToString();
            }
            catch (Exception e) when (e is ObjectDisposedException || e is NullReferenceException)
            {
                _logger.LogCritical("HTTPProxy (cache): object was either disposed or null, this is NOT supposed to happen.");
                _logger.LogCritical(e, "Exception was: ");
                _cache = new MemoryCache(new MemoryCacheOptions
                {
                    ExpirationScanFrequency = TimeSpan.FromMinutes(5)
                });
            }
        }

        private static string FormatCacheKey(string key, string type)
        {
            return $"services.httpproxy.{type}.{key}";
        }
    }
}