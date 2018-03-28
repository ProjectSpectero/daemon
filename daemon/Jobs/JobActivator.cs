using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Spectero.daemon.Jobs
{
    public class JobActivator : Hangfire.JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public JobActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type jobType)
        {
            return _serviceProvider.GetServices<IJob>()
                    .FirstOrDefault(job => job.GetType() == jobType);
        }
    }
}