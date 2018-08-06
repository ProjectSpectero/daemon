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
using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public class SetGlobalProperty : BaseJob
    {
        [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "Name of the Global Property")]
        private string Property { get; set; }

        [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "User's proposed value")]
        private string Value { get; set; }

        public override CommandResult Execute()
        {
            switch (Property?.ToLower())
            {
                case "debug":
                    AppConfig.Debug = bool.Parse(Value);
                    break;
                
                case "json":
                    AppConfig.OutputJson = bool.Parse((Value));
                    break;
                    
                default:
                    Console.WriteLine("Unrecognized property " + Property + " given, ignoring...");
                    return CommandResult.UsageError;
            }

            Console.WriteLine("Property " + Property + " was set to " + Value);
            return CommandResult.Success;
        }
        
        public override bool IsDataCommand() => true;
    }
}