using System;
using Microsoft.Extensions.Logging;

namespace Spectero.daemon.Jobs
{
    /*
        Use this class to test changes to the activation infrastructure, if needed
        It is not loaded to the IoC normally.
    */
    public class TestJob : IJob
    {
        private readonly ILogger<TestJob> _logger;

        public TestJob(ILogger<TestJob> logger)
        {
            _logger = logger;
        }

        public string GetSchedule()
        {
            return "*/1 * * * *";
        }

        public void Perform()
        {
            _logger.LogInformation(DateTime.Now.ToString());
        }

        public bool IsEnabled()
        {
            return true;
        }
    }
}