using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    internal class ConnectToCloudRequest : BaseRequest
    { 
    

        public ConnectToCloudRequest(IServiceProvider serviceProvider) : base (serviceProvider)
        {

        }

        public override APIResponse Perform(string requestBody = null)
        {
            var request = new RestRequest("cloud/connect", Method.POST) { RequestFormat = DataFormat.Json };
            var requestParams = new Dictionary<string, string>
            {
                {"nodeKey", requestBody}
            };

            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(requestParams), ParameterType.RequestBody);

            var response = _client.Execute(request);

            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}
