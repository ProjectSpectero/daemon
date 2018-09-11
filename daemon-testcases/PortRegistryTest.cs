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
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.PortRegistry;

namespace daemon_testcases
{
    public class PortRegistryTest : BaseUnitTest
    {
        private Mock<ILogger<PortRegistry>> mockedLogger;
        private Mock<IOptionsMonitor<AppConfig>> mockedMonitoredConfig;
        private readonly IPAddress testTarget = IPAddress.Parse("1.1.1.1");

        public PortRegistryTest()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            mockedLogger =  new Mock<ILogger<PortRegistry>>();
            
            mockedMonitoredConfig = new Mock<IOptionsMonitor<AppConfig>>();
            var enclosedConfig = new AppConfig
            {
                PortRegistry = new PortRegistryConfig {NATEnabled = false, NATDiscoveryTimeoutInSeconds = 10}
            };

            mockedMonitoredConfig.Setup(x => x.CurrentValue).Returns(enclosedConfig);            
        }

        [Test]
        public void EnsureWildcardCollision()
        {            
            var portRegistry = new PortRegistry(mockedMonitoredConfig.Object, mockedLogger.Object);
            
            // First pass allocations
            portRegistry.Allocate(IPAddress.Any, 8080, TransportProtocol.TCP);
            Assert.True(portRegistry.IsAllocated(IPAddress.Any, 8080, TransportProtocol.TCP, out _));
            
            // Listener above is on IPAddress.Any, so this should come back positive.
            Assert.True(portRegistry.IsAllocated(testTarget, 8080, TransportProtocol.TCP, out _));
            
            // But the protocol changing should turn it back negative.
            Assert.False(portRegistry.IsAllocated(testTarget, 8080, TransportProtocol.UDP, out _));

            // So should a port change
            Assert.False(portRegistry.IsAllocated(testTarget, 8081, TransportProtocol.TCP, out _));

            // Now, let's add a listener on randomIP
            portRegistry.Allocate(testTarget, 8081, TransportProtocol.UDP);

            // Allocation on IPAddress.Any for the same thing should now fail.
            Assert.Throws<InternalError>(() => portRegistry.Allocate(IPAddress.Any, 8081, TransportProtocol.UDP));

            // But allocation on another protocol should not fail.
            Assert.DoesNotThrow(() => portRegistry.Allocate(IPAddress.Any, 8081, TransportProtocol.TCP));
            
            Assert.AreEqual(3, portRegistry.GetAllAllocations().Count());

            portRegistry.CleanUp();
            Assert.AreEqual(0, portRegistry.GetAllAllocations().Count());
        }
    }
}