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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Nat;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.PortRegistry
{
    public class PortAllocation
    {
        public IPAddress Ip { get; set; }
        public int Port { get; set; }
        public TransportProtocol Protocol { get; set; }
        public IService Service { get; set; }        
    }
    
    public class PortRegistry : IPortRegistry
    {
        /*
         * The idea here is very simple. Every service, when starting up, will notify us that they need x port on y IP
         * We will then liaise with the local system firewall and router firewall (via UPNP) to effectively carry out that request
         * Support for carrying out "special" requests (such as MASQUERADE/SNAT) will eventually be added here as well (TBD).
         * We will also do conflict checking, the same port on the same IP (take IPAddress.Any into account for both v4/v6) may not be allocated multiple times.
         * A cleanup method will "deregister" all ports belonging to a service or the whole app (to be called at system shutdown)
         */

        private readonly ConcurrentDictionary<IService, IEnumerable<PortAllocation>> _serviceAllocations;
        private readonly IEnumerable<PortAllocation> _appAllocations;
        private readonly ILogger<PortRegistry> _logger;
        private readonly AppConfig _config;

        private NatDevice _device;
        // ReSharper disable once InconsistentNaming
        private IPAddress _externalIP;

        // This variable controls whether mapping entries should be attempted to be propagated to an upstream router
        // We start off with enabled, but if we can't find the router (or encounter other errors), we simply turn it off and fall back to simply being an internal port registry.
        private bool _natEnabled = true;

        public PortRegistry(IOptionsMonitor<AppConfig> configMonitor, ILogger<PortRegistry> logger)
        {
            _config = configMonitor.CurrentValue;
            _logger = logger;
            
            _serviceAllocations = new ConcurrentDictionary<IService, IEnumerable<PortAllocation>>();
            _appAllocations = new List<PortAllocation>();
            
            // Init internal state(s).
            Initialize();
        }

        // Separated from the constructor because this method may take a long time before timing out.
        private void Initialize()
        {
            _logger.LogDebug("Starting the NAT discovery process.");
            
            var nat = new NatDiscoverer();
            var cancellationToken = new CancellationTokenSource(_config.NatDiscoveryTimeoutInSeconds * 1000);

            try
            {
                _device = nat.DiscoverDeviceAsync(PortMapper.Upnp, cancellationToken).Result;
                _logger.LogDebug("Discovered the NAT device for this network.");
                
                _logger.LogDebug("Attempting to discover our external IP through the NAT device.");
                _externalIP = _device.GetExternalIPAsync().Result;
            
                _logger.LogDebug($"Discovered external IP (according to the NAT device) was: {_externalIP}");
            }
            catch (NatDeviceNotFoundException exception)
            {
                _device = null;
                _natEnabled = false;
                
                _logger.LogInformation($"No NAT devices could be found in time ({_config.NatDiscoveryTimeoutInSeconds} seconds)," +
                                       $" either this network does not require one (direct connectivity) or UPnP is NOT enabled." +
                                       " It may help to increase the timeout (NatDiscoveryTimeoutInSeconds in appsettings.json).");
            }
        }

        public PortAllocation Allocate(IPAddress ip, int port, IService forwardedFor = null)
        {
            throw new System.NotImplementedException();
        }

        public bool IsAllocated(IPAddress ip, int port, out PortAllocation allocation)
        {
            throw new System.NotImplementedException();
        }

        public bool IsAllocated(string ip, int port, out PortAllocation allocation)
        {
            throw new System.NotImplementedException();
        }

        public bool CleanUp(IService service = null)
        {
            throw new System.NotImplementedException();
        }
    }
}