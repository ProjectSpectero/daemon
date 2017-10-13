using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Spectero.daemon.Libraries.Core
{
    public class Utility
    {
        public static IEnumerable<IPNetwork> GetLocalRanges()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var ipNetworks = new List<IPNetwork>();

            foreach (var nic in nics)
            {
                var ipProps = nic.GetIPProperties();

                var ipAddresses = ipProps.UnicastAddresses;

                foreach (var addr in ipAddresses)
                    if (CheckIPFilter(addr))
                        ipNetworks.Add(IPNetwork.Parse(addr.Address + "/" + addr.PrefixLength));
            }
            return ipNetworks;
        }

        private static bool CheckIPFilter(UnicastIPAddressInformation ipAddressInformation)
        {
            var ret = true;
            var ipString = ipAddressInformation.Address.ToString();

            if (ipString.StartsWith("fe80:"))
                ret = false;
            else if (ipString.StartsWith("169.254"))
                ret = false;

            return ret;
        }
    }
}