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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Nat;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Libraries.PortRegistry
{
    public class PortAllocation
    {
        public IPAddress IP { get; set; }
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

        private readonly ConcurrentDictionary<IService, List<PortAllocation>> _serviceAllocations;
        private readonly List<PortAllocation> _appAllocations;
        private readonly ILogger<PortRegistry> _logger;
        private readonly AppConfig _config;

        private NatDevice _device;
        
        // ReSharper disable once InconsistentNaming
        private IPAddress _externalIP;

        // This variable controls whether mapping entries should be attempted to be propagated to an upstream router
        // We start off with enabled, but if we can't find the router (or encounter other errors), we simply turn it off and fall back to simply being an internal port registry.
        private bool _natEnabled = true;
        private bool _isInitialized = false;

        public PortRegistry(IOptionsMonitor<AppConfig> configMonitor, ILogger<PortRegistry> logger)
        {
            _config = configMonitor.CurrentValue;
            _logger = logger;
            
            _serviceAllocations = new ConcurrentDictionary<IService, List<PortAllocation>>();
            _appAllocations = new List<PortAllocation>();
            
            // Init internal state(s).
            Initialize();
        }

        // Separated from the constructor because this method may take a long time before timing out.
        private void Initialize()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
            
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
            else
                _logger.LogDebug("Skipping init, has been run before.");

        }

        private bool PropagateToRouter(PortAllocation allocation)
        {
            if (! _isInitialized)
                Initialize();

            if (!_natEnabled)
            {
                _logger.LogDebug($"Propagation requested for PortAllocation -> ({allocation.IP}:{allocation.Port} @ {allocation.Protocol}), but NAT is disabled!");
                return false;
                
            }
            
            Protocol translatedProtocol;
            
            switch (allocation.Protocol)
            {
                case TransportProtocol.TCP:
                    translatedProtocol = Protocol.Tcp;
                    break;
                    
                default:
                    translatedProtocol = Protocol.Udp;
                    break;
            }

            var description = "Spectero Daemon " + (allocation?.Service?.GetType()?.FullName ?? "Internal");

            try
            {
                // Let's try to make the actual mapping.
                _device
                    .CreatePortMapAsync(new Mapping(translatedProtocol, allocation.Port, allocation.Port, description))
                    .Wait();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not propagate PortAllocation to router");
                return false;
            }
            
            return true;            
        }

        public PortAllocation Allocate(IPAddress ip, int port, TransportProtocol protocol, IService forwardedFor = null)
        {
            if (IsAllocated(ip, port, protocol, out var allocation))
            {
                var belongsTo = allocation?.Service?.GetType().ToString() ?? "internally to the daemon.";
                throw new InternalError($"Port {port} ({protocol}) on {ip} is already allocated! It belongs to svc: {belongsTo}");
            }
            
            // OK, an allocation for this already does not exist. Let's do our thing.
            var portAllocation = new PortAllocation
            {
                IP = ip,
                Port = port,
                Protocol = protocol,
                Service = forwardedFor
            };
            
            // Where it needs to go is determined by whether we're doing it on behalf of a service, or for the overall app itself.
            if (forwardedFor != null)
            {
                if(_serviceAllocations.TryGetValue(forwardedFor, out var existingAllocations))
                {
                    existingAllocations.Add(portAllocation);
                }
                else
                {
                    // OK, we gotta init the list itself and include the first element into it. Then, we have to add it to the dictionary.
                    var newList = new List<PortAllocation> {portAllocation};
                    
                    if (! _serviceAllocations.TryAdd(forwardedFor, newList))
                        throw new InternalError("Could not add the PortAllocation to the concurrent tracker! This is NOT supposed to happen.");
                    
                }
            }
            else
            {
                // OK, this was NOT on behalf of a service.
                _appAllocations.Add(portAllocation);
            }
            
            // Let us attempt to propagate it into the local router too, if needed.
            PropagateToRouter(portAllocation);

            return portAllocation;
        }

        public bool IsAllocated(IPAddress ip, int port, TransportProtocol protocol, out PortAllocation allocation)
        {
            var matchingServiceAllocations = _serviceAllocations.Where(x => x.Value.Any(p => p.IP.Equals(ip) && p.Port == port && p.Protocol == protocol))
                .SelectMany(p => p.Value)
                .ToArray();

            if (matchingServiceAllocations.Any())
            {
                // OK, at least one match was found.
                allocation = matchingServiceAllocations.First();
                return true;
            }
            
            // If we got here, it wasn't found as a service allocation.
            var matchingApplicationAllocations =
                _appAllocations.Where(x => x.IP.Equals(ip) && x.Port == port && x.Protocol == protocol)
                .ToArray();

            if (matchingApplicationAllocations.Any())
            {
                // OK, a match was found in the internal allocations registry.
                allocation = matchingApplicationAllocations.First();
                return true;
            }

            allocation = null;
            return false;
        }

        public bool IsAllocated(string ip, int port, TransportProtocol protocol, out PortAllocation allocation)
        {
            // ReSharper disable once InconsistentNaming
            if (IPAddress.TryParse(ip, out var parsedIP))
                return IsAllocated(parsedIP, port, protocol, out allocation);
            
            throw new InternalError($"Unparseable IP ({ip}) given, aborting!");
        }

        public bool CleanUp(IService service = null)
        {
            throw new System.NotImplementedException();
        }
        
        
    }
}