﻿/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System.Collections.Generic;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class ScopedAuthentication : BaseJob
    {
        // TODO: Figure out why NamedArguments didn't work here, NCAP bug possibly?
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The service scope being requested from the system for the user. Usually one of { HTTPProxy | OpenVPN | SSHTunnel | ShadowSOCKS }")]
        private string Scope { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "The username being attempted")]
        private string Username { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 2, Description = "The user's password")]
        private string Password { get; set; }


        public override CommandResult Execute()
        {
            var request = new AuthenticationRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"authKey", Username},
                {"password", Password },
                {"serviceScope", Scope }
            };

            return HandleRequest(null, request, body, caller: this);
        }
        
        public override bool IsDataCommand() => true;
    }
}
