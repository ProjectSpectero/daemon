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
            return ActualPerform("cloud/manual", Method.POST, requestBody);
        }
    }
}
