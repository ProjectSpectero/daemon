/*
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
using System;
using System.Collections.Generic;
using System.IO;
using NClap.Metadata;
using Spectero.daemon.CLI.Requests;

namespace Spectero.daemon.CLI.Commands
{
    public class InlineFileAuth : BaseJob
    {
        [PositionalArgument(
            ArgumentFlags.Required,
            Position = 0,
            Description = "The service scope being requested from the system for the user. Usually one of { HTTPProxy | OpenVPN | SSHTunnel | ShadowSOCKS }"
        )]
        private string Scope { get; set; }

        [PositionalArgument(
            ArgumentFlags.Required,
            Position = 1,
            Description = "The name of the file with authentication data."
        )]
        private string Filename { get; set; }
        
        public override bool IsDataCommand() => true;

        public override CommandResult Execute()
        {
            string[] configFileObject;
            string pluckedUsername;
            string pluckedPassword;

            // Attempt to read the config.
            try
            {
                // Read.
                configFileObject = File.ReadAllLines(Filename);

                // Pluck the data in order
                pluckedUsername = configFileObject[0];
                pluckedPassword = configFileObject[1];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return CommandResult.RuntimeFailure;
            }

            // Build the request
            var request = new AuthenticationRequest(ServiceProvider);
            var body = new Dictionary<string, object>
            {
                {"authKey", pluckedUsername},
                {"password", pluckedPassword},
                {"serviceScope", Scope}
            };

            // Run it.
            return HandleRequest(null, request, body, caller: this);
        }
    }
}