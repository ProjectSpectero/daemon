using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using Spectero.daemon.Libraries.APM;
using Assert = NUnit.Framework.Assert;

namespace daemon_testcases
{
    [TestFixture]
    public class ApmUnitTest
    {
        [Test]
        public void IsSerializable()
        {
            try
            {
                // Instantiate new APM
                Apm localApm = new Apm();

                // Get the details we need
                var details = localApm.GetAllDetails();

                // Here's what matters, can we serialize it?
                JsonConvert.SerializeObject(details, Formatting.Indented);

                // If so, the test case will be considered as passing.
            }
            catch (Exception exception)
            {
                Assert.Fail(string.Format("Exception occured while attempting to serialize: {0}", exception));
            }
        }
    }
}