using System;
using System.Collections.Generic;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public class DisconnectFromSpecteroCloudRequest : BaseRequest
    {
        public DisconnectFromSpecteroCloudRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            
        }

        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            return ActualPerform("cloud/disconnect", Method.POST);
        }
    }
}