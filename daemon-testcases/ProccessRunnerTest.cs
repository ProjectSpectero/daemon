using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;

namespace daemon_testcases
{
    [TestFixture]
    public class ProccessRunnerTest : BaseUnitTest
    {
        [Test]
        public void TestMonitoring()
        {
            var svcMock = new Mock<IService>();
            svcMock.Setup(x => x.GetState()).Returns(ServiceState.Running);

            var loggerMock = new Mock<ILogger<ProcessRunner>>();

            var configMonitorMock = new Mock<IOptionsMonitor<AppConfig>>();
            configMonitorMock.Setup(x => x.CurrentValue).Returns(new AppConfig());


            // Get a process runner going.
            var processRunner = new ProcessRunner(configMonitorMock.Object, loggerMock.Object);

            // Build the command options
            var processOptions = new ProcessOptions()
            {
                Executable =
                    AppConfig.isUnix
                        ? "top"
                        : "cmd", // top and cmd are both processes that will run continuously until closed.
                DisposeOnExit = false,
                Monitor = true,
                MonitoringInterval = 5
            };

            // Run the example command.
            var runningProcess = processRunner.Run(processOptions, svcMock.Object);
            var oldPid = runningProcess.Command.ProcessId;

            // Sleep 5000 ms before killing it
            Thread.Sleep(5000);

            runningProcess.Command.Kill();

            // Now we wait 10 seconds for it to restart by itself
            Thread.Sleep(10 * 1000);
            var newPid = runningProcess.Command.ProcessId;

            Assert.AreNotEqual(oldPid, newPid);
        }
    }
}