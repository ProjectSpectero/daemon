using System;
using System.Collections.Generic;
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
            return ActualPerform("cloud/connect", Method.POST, requestBody);
        }
    }
}
