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
    public class Version : BaseJob
    {
        public override CommandResult Execute()
        {
            Console.WriteLine("Spectero Console v{0} with Spectero Daemon v{1}", AppConfig.version, daemon.Libraries.Config.AppConfig.Version);
            Console.WriteLine("Copyright (c) 2017 - {0}, Spectero, Inc.", DateTime.Now.Year);

            return CommandResult.Success;
        }
        
        public override bool IsDataCommand() => true;
    }
}