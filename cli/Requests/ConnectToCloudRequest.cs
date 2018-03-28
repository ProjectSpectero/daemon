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

        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            var request = new RestRequest("cloud/connect", Method.POST) { RequestFormat = DataFormat.Json };

            if (requestBody != null)
                request.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);

            var response = Client.Execute(request);

            return ParseResponse<APIResponse>(response);
        }
    }
}
