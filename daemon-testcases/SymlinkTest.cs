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
            var symlinkLib = new Symlink(_runner);

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