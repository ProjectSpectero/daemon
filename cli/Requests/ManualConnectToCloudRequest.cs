using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    class ManualConnectToCloudRequest : BaseRequest
    {

        public ManualConnectToCloudRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            var request = new RestRequest("cloud/manual", Method.POST) { RequestFormat = DataFormat.Json };

            request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);

            var response = _client.Execute(request);

            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}
