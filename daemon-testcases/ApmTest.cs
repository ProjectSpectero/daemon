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
using Newtonsoft.Json;
using NUnit.Framework;
using Spectero.daemon.Libraries.APM;

namespace daemon_testcases
{
    /*
     * Application Performance Management
     * This class is dedicated to testing the serialization of the host's operating system dictionary set.
      */

    [TestFixture]
    public class ApmTest : BaseUnitTest
    {
        [Test]
        public void IsSerializable()
        {
            // Instantiate new APM
            var localApm = new Apm();

            // Get the details we need
            var details = localApm.GetAllDetails();

            // Here's what matters, can we serialize it?
            // Printed out too, so we can visually inspect output.
            Console.WriteLine(JsonConvert.SerializeObject(details, Formatting.Indented));
        }
    }
}