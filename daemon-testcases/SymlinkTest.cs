using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Symlink;

namespace daemon_testcases
{
    [TestFixture]
    public class SymlinkTest
    {
        private Mock<IService> mockedService;
        private Mock<ILogger<ProcessRunner>> mockedLogger;
        private Mock<IOptionsMonitor<AppConfig>> mockedMonitoredConfig;
        private readonly ProcessRunner _runner;

        public SymlinkTest()
        {
            mockedService = new Mock<IService>();
            mockedService.Setup(x => x.GetState()).Returns(ServiceState.Running);
            mockedLogger = new Mock<ILogger<ProcessRunner>>();
            mockedMonitoredConfig = new Mock<IOptionsMonitor<AppConfig>>();
            mockedMonitoredConfig.Setup(x => x.CurrentValue).Returns(new AppConfig());
            _runner = new ProcessRunner(mockedMonitoredConfig.Object, mockedLogger.Object);
        }

        [Test]
        // Example function to test if the Windows API for Symlink Creation is working.
        public void CreateAndDelete()
        {
            var tempPath = Path.GetTempPath();
            var tempPathSymlink = Path.Combine(tempPath, "symlink");

            // Initialize the Symlink Library
            var symlinkLib = new Symlink();
            symlinkLib.SetProcessRunner(_runner);

            // Try to create the symlink.
            if (symlinkLib.GetEnvironment().Create(tempPathSymlink, tempPath))
            {
                // Symlink creation was successful - delete it and pass.
                symlinkLib.GetEnvironment().Delete(tempPathSymlink);
            }
            else
            {
                // Symlink failed to be created
                Assert.Fail();
            }
        }
    }
}