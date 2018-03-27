using System;
using System.Collections.Generic;
using System.Text;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.CLI.Requests
{
    class ManualConnectToCloudRequest : BaseRequest
    {

        public ManualConnectToCloudRequest(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        public override APIResponse Perform(string requestBody = null)
        {
            return base.Perform(requestBody);
        }
    }
}
