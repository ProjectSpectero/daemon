using System;
using System.Collections.Generic;
using RestSharp;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    public class ShutdownRequest : BaseRequest
    {
        public ShutdownRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            
        }
        
        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            return ActualPerform("system/shutdown", Method.POST, requestBody);
        }
    }
}
