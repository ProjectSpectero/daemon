using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public class GetCloudConnectivityStatusRequest : BaseRequest
    {

        public GetCloudConnectivityStatusRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        // Synchronus for now
        // TODO: @alex - write the other cloud related daemon endpoint requests too.
        public override APIResponse Perform(string requestBody = null)
        {
            var request = new RestRequest("/cloud", Method.GET);

            // Body is irrelevant here
            var response = _client.Execute(request);
            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}