using NUnit.Framework;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Services.OpenVPN;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace daemon_testcases
{
    [TestFixture]
    class OpenVPNTest
    {
        /*
          OpenVPN Test Cases
          This class is incomplete, and further discussion is required about initializing OpneVPN.cs from the daemon.
        */

        [Test]
        public void RestartMechanism()
        {
            // Initialize a new OpenVPN Instance.
            var ovpn = new OpenVPN();

            // Start everything.
            ovpn.Start();

            // Wait some time
            Thread.Sleep(10 * 1000);

            // Attempt to restart the processes.
            ovpn.ReStart();

            // Wait some time
            Thread.Sleep(10 * 1000);

            // Stop and mark the test as complete.
            ovpn.Stop();
        }

    }
}
