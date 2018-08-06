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
using NClap.Metadata;

namespace Spectero.daemon.CLI.Commands
{
    public enum Commands
    {
        [Command(typeof(ScopedAuthentication), Description = "Service specific authentication helper, meant for invocation by 3rd party binaries.")]
        auth,

        [Command(typeof(ConnectToSpecteroCloud), Description = "Automagic connection to the Spectero Cloud")]
        connect,

        [Command(typeof(DisconnectFromSpecteroCloud), Description = "Disconnect from the Spectero Cloud (client only)")]
        disconnect,

        [Command(typeof(SetGlobalProperty), Description = "Sets various system properties, consult the docs for details.")]
        env,

        [Command(typeof(GetSystemHeartbeat), Description = "Check if the Daemon is online and connectible")]
        heartbeat,

        [Command(typeof(ManuallyConnectToSpecteroCloud), Description = "Manually Connect Daemon to Spectero Cloud")]
        manual,

        [Command(typeof(ViewCloudConnectivityStatus), Description = "See the current state of connectivity to the Spectero Cloud")]
        status,

        [Command(typeof(Shell), Description = "Invokes the Spectero Shell")]
        shell,

        [Command(typeof(Version), Description = "Shows the Spectero CLI and Linked Daemon Versions")]
        version,
        
        [Command(typeof(InlineFileAuth), Description = "Service specific authentication helper, meant for invocation by 3rd party binary: OpenVPN")]
        fileauth,

        /*
         * Shutdown command
         * Commenting out for AUTH requirement.
         * 
         * [Command(typeof(Shutdown), Description = "Shutdown the spectero daemon")]
         * shutdown,
         */
    }
}