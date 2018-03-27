using System;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public abstract class BaseRequest : IRequest
    {
        private readonly IServiceProvider _serviceProvider;
        protected readonly IRestClient _client;

        protected BaseRequest(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _client = serviceProvider.GetService<IRestClient>();

        }

        public virtual APIResponse Perform(string requestBody = null)
        {
            throw new NotImplementedException();
        }
    }
}