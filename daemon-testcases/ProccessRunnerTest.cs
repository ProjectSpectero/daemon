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

using System.Threading;
using Medallion.Shell;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services;
using Assert = NUnit.Framework.Assert;

namespace daemon_testcases
{
    [TestFixture]
    public class ProccessRunnerTest : BaseUnitTest
    {
        private Mock<IService> mockedService;
        private Mock<ILogger<ProcessRunner>> mockedLogger;
        private Mock<IOptionsMonitor<AppConfig>> mockedMonitoredConfig;

        private readonly ProcessRunner _runner;
        
        public ProccessRunnerTest ()
        {
            mockedService = new Mock<IService>();
            mockedService.Setup(x => x.GetState()).Returns(ServiceState.Running);
            
            mockedLogger =  new Mock<ILogger<ProcessRunner>>();
            
            mockedMonitoredConfig = new Mock<IOptionsMonitor<AppConfig>>();
            mockedMonitoredConfig.Setup(x => x.CurrentValue).Returns(new AppConfig());
            
            _runner = new ProcessRunner(mockedMonitoredConfig.Object, mockedLogger.Object);
        }
        
        [Test]
        public void TestMonitoring()
        {
            Assert.AreEqual(mockedService.Object.GetState(), ServiceState.Running);

            // Build the command options
            var processOptions = new ProcessOptions()
            {
                Executable =
                    AppConfig.isUnix
                        ? "top"
                        : "cmd", // top and cmd are both processes that will run continuously until closed.
                DisposeOnExit = false,
                Monitor = true,
                MonitoringInterval = 5,
                ThrowOnError = true
            };

            // Run the example command.
            var runningProcess = _runner.Run(processOptions, mockedService.Object);
            var oldPid = runningProcess.Command.Process.Id;

            runningProcess.Command.Kill();

            // Now we wait monitoringInterval + 1 seconds for it to restart by itself
            Thread.Sleep((processOptions.MonitoringInterval + 1) * 1000);
            var newPid = runningProcess.Command.Process.Id;

            Assert.AreNotEqual(oldPid, newPid);
        }

        [Test]
        public void TestThrowOnErrorBehavior()
        {
            var processOptions = new ProcessOptions()
            {
                Executable =
                    AppConfig.isUnix
                        ? "top"
                        : "powershell", // top and cmd are both processes that will run continuously until closed.
                DisposeOnExit = false,
                Monitor = true,
                MonitoringInterval = 5,
                ThrowOnError = true,
                Arguments = new[] { "this-does-not-exist" }
            };

            var holder = _runner.Run(processOptions, mockedService.Object);

            Assert.Throws<ErrorExitCodeException>(() => holder.Command.Wait());
        }
    }
}