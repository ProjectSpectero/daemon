﻿using NClap.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectero.daemon.CLI.Requests;
using System;
using System.Collections.Generic;
using RestSharp;

namespace Spectero.daemon.CLI.Commands
{
    public class ManuallyConnectToSpecteroCloud : BaseJob
    {

        [NamedArgument(ArgumentFlags.Required, ShortName = "id", Description = "The node's Id")]
        private string NodeId { get; set; }

        [NamedArgument(ArgumentFlags.Required, ShortName = "key", Description = "The node's key")]
        private string NodeKey { get; set; }

        [NamedArgument(ArgumentFlags.Optional, ShortName = "force", Description = "Force connect")]
        private bool ForceConnect { get; set; }

        public override CommandResult Execute()
        {
            var request = new ManualConnectToCloudRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"force", ForceConnect },
                {"nodeId", NodeId },
                {"nodeKey", NodeKey }
            };

            return HandleRequest(null, request, body);
        }
    }
}