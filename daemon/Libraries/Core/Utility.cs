using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Medallion.Shell;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Extensions;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

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

                // addr.PrefixLength is not available on Unix (https://github.com/dotnet/corefx/blob/35d0838c20965c526e05e119028dd7226084987c/src/System.Net.NetworkInformation/src/System/Net/NetworkInformation/UnixUnicastIPAddressInformation.cs#L47)
                // So much for TRUE multiplatform, eh?
                foreach (var addr in ipAddresses)
                {
                    if (CheckIPFilter(addr, IPComparisonReasons.FOR_LOCAL_NETWORK_PROTECTION))
                    {
                        IPNetwork network = null;
                        if (AppConfig.isWindows)
                            network = IPNetwork.Parse(addr.Address + "/" + addr.PrefixLength);                           
                        else if (AppConfig.isUnix)
                            network = IPNetwork.Parse(addr.Address + "/" + GetPrefixLengthFromNetmask(addr.IPv4Mask));
                        else
                        {
                            // Well, giggity. If it ain't Windows OR Unix, then WHAT is it?
                            continue;
                        }
                        ipNetworks.Add(network);
                    }                       
                }
                   
            }
            return ipNetworks;
        }

        public static IEnumerable<IPAddress> GetLocalIPs(bool ignoreRFC1918 = false)
        {
            var output = new List<IPAddress>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = nic.GetIPProperties();
                var addresses = properties.UnicastAddresses;
                var selection = addresses
                    .Where(t => CheckIPFilter(t, IPComparisonReasons.FOR_PROXY_OUTGOING));
                output.AddRange(selection.Select(unicastIpAddressInformation => unicastIpAddressInformation.Address));
            }

            return ignoreRFC1918 ? output.Where(x => !x.IsInternal()) : output;
        }

        private static int GetPrefixLengthFromNetmask(IPAddress netmask)
        {
            var str = netmask.GetAddressBytes().Select(x => Convert.ToString(int.Parse(x.ToString()), 2).PadLeft(8, '0'));

            return str.Sum(element => element.Count(x => x.Equals('1')));
        }

        public enum IPComparisonReasons
        {
            FOR_PROXY_OUTGOING,
            FOR_LOCAL_NETWORK_PROTECTION
        }

        public static bool CheckIPFilter(IPAddress address, IPComparisonReasons reason)
        {
            var ipString = address.ToString();
            var ret = true;

            if (ipString.StartsWith("fe"))
                ret = false;
            else if (ipString.StartsWith("169.254"))
                ret = false;
            else if (ipString.StartsWith("::"))
                ret = false;

            if (ret && reason == IPComparisonReasons.FOR_PROXY_OUTGOING)
            {
                if (ipString.StartsWith("127"))
                    ret = false;
                else if (ipString.StartsWith("fc")
                         || ipString.StartsWith("fd")
                         || ipString.StartsWith("fe"))
                    ret = false;
                else if (ipString.Equals("0.0.0.0"))
                    ret = false;
            }

            return ret;
        }

        private static bool CheckIPFilter(UnicastIPAddressInformation ipAddressInformation, IPComparisonReasons reason)
        {
            return CheckIPFilter(ipAddressInformation.Address, reason);
        }

        public static IEnumerable<HttpHeader> ExtractHeader(HeaderCollection headers, string headerName)
        {
           return ((IEnumerable<HttpHeader>) headers.ToArray<HttpHeader>())
                .Where<HttpHeader>((Func<HttpHeader, bool>)
                    (
                        t => t.Name == headerName
                    )
                );
        }

        public static Type GetServiceType(string name)
        {
            Type ret;
            switch (name)
            {
                case "HTTPProxy":
                    ret = typeof(Services.HTTPProxy.HTTPProxy);
                    break;
                case "OpenVPN":
                    ret = typeof(Services.OpenVPN.OpenVPN);
                    break;
                case "SSHTunnel":
                    ret = null;
                    break;
                case "ShadowSOCKS":
                    ret = null;
                    break;
                default:
                    throw new ValidationError();
            }
            return ret;
        }

        public static string GetCurrentStartupMarker()
        {
            var currentTIme = DateTime.UtcNow;
            var minuteMarker = currentTIme.Minute.ToString()[0];

            return Path.Combine(Path.GetTempPath(),
                $"spectero-startup-{currentTIme.Year}-{currentTIme.Month}-{currentTIme.Day}-{minuteMarker}");
        }

        public static bool ManageStartupMarker(bool delete = false)
        {
            var marker = GetCurrentStartupMarker();

            if (delete)
            {
                if (File.Exists(marker))
                    File.Delete(marker);
                else
                    return false;
            }
            else
            {
                File.Create(marker).Dispose();
            }

            return true;
        }

    }
}