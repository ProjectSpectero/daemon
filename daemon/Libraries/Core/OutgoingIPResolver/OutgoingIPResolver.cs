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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Libraries.Core.OutgoingIPResolver
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class OutgoingIPResolver : IOutgoingIPResolver
    {
        private IPAddress _address;
        private readonly AppConfig _appConfig;

        public OutgoingIPResolver(IOptionsMonitor<AppConfig> configMonitor)
        {
            _appConfig = configMonitor.CurrentValue;
            _address = null;
        }

        public async Task<IPAddress> Resolve()
        {
            if (_address != null)
                return _address;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Spectero Daemon");
            var ipAddressString = await client.GetStringAsync(_appConfig.DefaultOutgoingIPResolver);

            if (IPAddress.TryParse(ipAddressString, out var parsedAddress))
                _address = parsedAddress;

            return _address;
        }

        public async Task<IPAddress> Translate(IPAddress ipAddress)
        {
            if (ipAddress.Equals(IPAddress.Any) || ipAddress.Equals(IPAddress.Loopback))
                return await Resolve();

            return ipAddress;
        }

        public async Task<IPAddress> Translate(string stringAddress)
        {
            if (IPAddress.TryParse(stringAddress, out var parsedAddress))
                return await Translate(parsedAddress);

            return null;
        }
    }
}