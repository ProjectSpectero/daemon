using System;
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
        public void WindowsSymlinkExample()
        {
            if (AppConfig.isWindows)
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appDataSymlink = Path.Combine(appData, "symlink");

                var symlinkLib = new Symlink();
                symlinkLib.processRunner = _runner;
                if (symlinkLib.Environment.Create(appDataSymlink, appData))
                {
                    // Symlink creation was successful - delete it and pass.
                    symlinkLib.Environment.Delete(appDataSymlink);
                    Assert.Pass();
                }
                else
                {
                    // Symlink failed to be created
                    Assert.Fail();
                }
            }

            Assert.Pass("Linux Detected - Asserting as true for this test case.");
        }
    }
}