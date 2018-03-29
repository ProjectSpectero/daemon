using System;
using System.Collections.Generic;
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
        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            return ActualPerform("cloud", Method.GET, requestBody);
        }
    }
}