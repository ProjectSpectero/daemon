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
        private IPortRegistry PortRegistry;
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
                PortRegistry = new PortRegistryConfig {NATEnabled = true, NATDiscoveryTimeoutInSeconds = 10}
            };

            mockedMonitoredConfig.Setup(x => x.CurrentValue).Returns(enclosedConfig);
            
            
            PortRegistry = new PortRegistry(mockedMonitoredConfig.Object, mockedLogger.Object);
        }

        [Test]
        public void EnsureWildcardAllocation()
        {
            // First pass allocations
            PortRegistry.Allocate(IPAddress.Any, 8080, TransportProtocol.TCP);
            Assert.True(PortRegistry.IsAllocated(IPAddress.Any, 8080, TransportProtocol.TCP, out _));
        }

        [Test]
        public void EnsureWildcardCollision()
        {            
            // Listener above is on IPAddress.Any, so this should come back positive.
            Assert.True(PortRegistry.IsAllocated(testTarget, 8080, TransportProtocol.TCP, out _));
        }

        [Test]
        public void EnsureCorrectness()
        {

            // But the protocol changing should turn it back negative.
            Assert.False(PortRegistry.IsAllocated(testTarget, 8080, TransportProtocol.UDP, out _));
            
            // So should a port change
            Assert.False(PortRegistry.IsAllocated(testTarget, 8081, TransportProtocol.TCP, out _));
            
            // Now, let's add a listener on randomIP
            PortRegistry.Allocate(testTarget, 8081, TransportProtocol.UDP);
            
            // Allocation on IPAddress.Any for the same thing should now fail.
            Assert.Throws<InternalError>(() => PortRegistry.Allocate(IPAddress.Any, 8081, TransportProtocol.UDP));
            
            // But allocation on another protocol should not fail.
            Assert.DoesNotThrow(() => PortRegistry.Allocate(IPAddress.Any, 8081, TransportProtocol.TCP));
        }
    }
}