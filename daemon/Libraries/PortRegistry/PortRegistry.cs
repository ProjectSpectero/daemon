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
using Microsoft.Extensions.Logging;
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
        private readonly List<PortAllocation> _appAllocations;
        private readonly ILogger<PortRegistry> _logger;

        public PortRegistry(ILogger<PortRegistry> logger)
        {
            _logger = logger;
            _serviceAllocations = new ConcurrentDictionary<IService, IEnumerable<PortAllocation>>();
            _appAllocations = new List<PortAllocation>();
        }
        

        public bool Allocate(IPAddress ip, int port, IService forwardedFor = null)
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