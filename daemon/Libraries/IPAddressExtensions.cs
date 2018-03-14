using System.Net;
using System.Net.Sockets;

namespace Spectero.daemon.Libraries
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