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
                Apm localApm = new Apm(GetMockLogger());
                var details = localApm.GetAllDetails();
                JsonConvert.SerializeObject(details, Formatting.Indented);
            }
            catch (Exception exception)
            {
                Assert.Fail(String.Format("Exception occured while attempting to serialize: {0}", exception));
            }
        }

        public Logger<Apm> GetMockLogger()
        {
            return Mock.Of<Logger<Apm>>();
        }
    }
}