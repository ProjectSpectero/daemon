﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
                // TODO: Figure out alternative implementation for Unix
                foreach (var addr in ipAddresses)
                    if (CheckIPFilter(addr, IPComparisonReasons.FOR_LOCAL_NETWORK_PROTECTION))
                        ipNetworks.Add(IPNetwork.Parse(addr.Address + "/" + addr.PrefixLength));
            }
            return ipNetworks;
        }

        public static IEnumerable<IPAddress> GetLocalIPs()
        {
            var output = new List<IPAddress>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = nic.GetIPProperties();
                var addresses = properties.UnicastAddresses;
                var selection = addresses
                    .Where<UnicastIPAddressInformation>((Func<UnicastIPAddressInformation, bool>)
                        (
                            t => CheckIPFilter(t, IPComparisonReasons.FOR_PROXY_OUTGOING)
                        )
                    );
                foreach (var unicastIpAddressInformation in selection)
                {
                    output.Add(unicastIpAddressInformation.Address);
                }
            }

            return output;
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

            if (ipString.StartsWith("fe80:"))
                ret = false;
            else if (ipString.StartsWith("169.254"))
                ret = false;

            if (ret && reason == IPComparisonReasons.FOR_PROXY_OUTGOING)
            {
                if (ipString.StartsWith("127"))
                    ret = false;
                else if (ipString.Equals("::1"))
                    ret = false;
                else if (ipString.Equals("0.0.0.0"))
                    ret = false;
            }

            return ret;
        }
        public static bool CheckIPFilter(UnicastIPAddressInformation ipAddressInformation, IPComparisonReasons reason)
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
    }
}