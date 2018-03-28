using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public abstract class BaseRequest : IRequest
    {
        private readonly IServiceProvider _serviceProvider;
        protected readonly IRestClient Client;

        protected BaseRequest(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Client = serviceProvider.GetService<IRestClient>();

        }

        public virtual APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            throw new NotImplementedException();
        }

        protected static T ParseResponse<T>(IRestResponse response)
        {
            if (response.ErrorException != null)
                throw response.ErrorException;

            return JsonConvert.DeserializeObject<T>(response.Content);
        }
    }
}