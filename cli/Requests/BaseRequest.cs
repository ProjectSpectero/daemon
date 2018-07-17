using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;
using Utility = Spectero.daemon.Libraries.Core.Utility;

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

        protected APIResponse ActualPerform(string endpoint, Method method, Dictionary<string, object> requestBody = null)
        {
            ValidateDaemonStartup();
            
            var request = new RestRequest(endpoint, method) { RequestFormat = DataFormat.Json };

            if (requestBody != null)
                request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);

            var response = Client.Execute(request);

            return ParseResponse<APIResponse>(response);
        }

        protected static T ParseResponse<T>(IRestResponse response)
        {
            if (response.ErrorException != null)
                throw response.ErrorException;

            return JsonConvert.DeserializeObject<T>(response.Content);
        }

        private static void ValidateDaemonStartup()
        {
            var marker = Utility.GetCurrentStartupMarker();
            
            if (File.Exists(marker))
                throw new Exception($"Spectero Daemon currently seems to be starting up, and is not ready to service requests yet (marker: {marker}). Please try again later.");
        }
    }
}