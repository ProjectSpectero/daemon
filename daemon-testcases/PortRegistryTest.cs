using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.PortRegistry;

namespace daemon_testcases
{
    public class PortRegistryTest : BaseUnitTest
    {
        private Mock<ILogger<PortRegistry>> mockedLogger;
        private Mock<IOptionsMonitor<AppConfig>> mockedMonitoredConfig;
        private IPortRegistry PortRegistry;

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
        public void EnsureCorrectness()
        {
            // TODO: Write the actual test cases.
        }
    }
}