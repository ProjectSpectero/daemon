﻿using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class DisconnectFromSpecteroCloud : BaseJob
    {
        public override CommandResult Execute()
        {
            var request = new DisconnectFromSpecteroCloudRequest(ServiceProvider);
            return HandleRequest(null, request, caller: this);
        }
        
        public override bool IsDataCommand() => false;
    }
}