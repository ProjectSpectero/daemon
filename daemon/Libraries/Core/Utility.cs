using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;

namespace Spectero.daemon.Libraries.Core
{
    public class Utility
    {
        public static IEnumerable<IPNetwork> GetLocalRanges()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            List<IPNetwork> ipNetworks = new List<IPNetwork>();

            foreach (var nic in nics)
            {
                var ipProps = nic.GetIPProperties();

                var ipAddresses = ipProps.UnicastAddresses;

                foreach (var addr in ipAddresses)
                {
                    if (CheckIPFilter(addr))
                        ipNetworks.Add(IPNetwork.Parse(addr.Address + "/" + addr.PrefixLength));
                }
            }
            return ipNetworks;
        }

        private static bool CheckIPFilter(UnicastIPAddressInformation ipAddressInformation)
        {
            var ret = true;
            var ipString = ipAddressInformation.Address.ToString();

            if (ipString.StartsWith("127"))
                ret = false;
            else if (ipString.StartsWith("fe80:"))
                ret = false;
            else if (ipString.StartsWith("169.254"))
                ret = false;
            else if (ipString.StartsWith("::1"))
                ret = false;

            return ret;
        }

        public static double GetObjectSize(object obj)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, obj);
            var array = ms.ToArray();
            return array.Length;
        }
    }
}