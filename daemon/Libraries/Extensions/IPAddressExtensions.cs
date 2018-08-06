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
using System.Net;
using System.Net.Sockets;

namespace Spectero.daemon.Libraries.Extensions
{
    public static class IPAddressExtensions
    {
        /* An IP should be considered as internal when:
             ::1          -   IPv6  loopback
             10.0.0.0     -   10.255.255.255  (10/8 prefix)
             127.0.0.0    -   127.255.255.255  (127/8 prefix)
             172.16.0.0   -   172.31.255.255  (172.16/12 prefix)
             192.168.0.0  -   192.168.255.255 (192.168/16 prefix)
        */
        public static bool IsInternal (this IPAddress address)
        {
            if (! address.AddressFamily.Equals(AddressFamily.InterNetwork))
                return false;

            var ip = address.GetAddressBytes();
            switch (ip[0])
            {
                case 10:
                case 127:
                    return true;
                case 172:
                    return ip[1] >= 16 && ip[1] < 32;
                case 192:
                    return ip[1] == 168;
                default:
                    return false;
            }
        }
    }
}