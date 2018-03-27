using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    class GetSystemHeartbeatRequest : BaseRequest
    {

        public GetSystemHeartbeatRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        public override APIResponse Perform(string requestBody = null)
        {
            var request = new RestRequest("/cloud/heartbeat", Method.GET);

            var response = _client.Execute(request);
            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}
