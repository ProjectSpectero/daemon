using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public class GetCloudConnectivityStatusRequest : IRequest
    {
        private readonly IRestClient client;

        public GetCloudConnectivityStatusRequest(IRestClient client)
        {
            this.client = client;
        }

        // Synchronus for now
        // TODO: @alex - write the other cloud related daemon endpoint requests too.
        public APIResponse Perform(string requestBody = null)
        {
            var request = new RestRequest("/cloud", Method.GET);

            // Body is irrelevant here
            var response = client.Execute(request);
            return JsonConvert.DeserializeObject<APIResponse>(response.Content);
        }
    }
}