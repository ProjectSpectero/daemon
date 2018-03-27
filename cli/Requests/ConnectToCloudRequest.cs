using System;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    class ConnectToCloudRequest : IRequest
    {
        private readonly IRestClient client;

        public ConnectToCloudRequest(IRestClient client)
        {
            this.client = client;
        }

        public APIResponse Perform(string requestBody = null)
        {
            var request = new RestRequest("cloud/connect", Method.POST);
            request.AddParameter("nodeKey", requestBody);

            var response = client.Execute(request);
            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}
