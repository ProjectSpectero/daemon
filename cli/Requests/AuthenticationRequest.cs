using System;
using System.Collections.Generic;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    class AuthenticationRequest : BaseRequest
    {
        public AuthenticationRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override APIResponse Perform(Dictionary<string, object> requestBody = null)
        {
            return ActualPerform("auth", RestSharp.Method.POST, requestBody);
        }
    }
}
