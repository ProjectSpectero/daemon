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
using System.Diagnostics;
using System.Threading;
using Medallion.Shell;
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

            Assert.AreEqual(svcMock.Object.GetState(), ServiceState.Running);


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
                MonitoringInterval = 5,
            };

            // Run the example command.
            var runningProcess = processRunner.Run(processOptions, svcMock.Object);
            var oldPid = runningProcess.Command.Process.Id;

            runningProcess.Command.Kill();

            // Now we wait monitoringInterval + 1 seconds for it to restart by itself
            Thread.Sleep((processOptions.MonitoringInterval + 1) * 1000);
            var newPid = runningProcess.Command.Process.Id;

            Assert.AreNotEqual(oldPid, newPid);
        }

        [Test]
        public void TestThrowOnError()
        {
            try
            {
                var cmd = Command.Run(AppConfig.isUnix ? "top" : "powershell", new[] {"--testing"}, options: o => o.ThrowOnError());
                Assert.Fail();
            }
            catch (Exception exception)
            {
                // Error means that the command execution broke. 
                Assert.Pass();
            }
        }
    }
}