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

        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            var request = new RestRequest("/cloud/heartbeat", Method.GET);

            var response = Client.Execute(request);

            return ParseResponse<APIResponse>(response);
        }
    }
}
