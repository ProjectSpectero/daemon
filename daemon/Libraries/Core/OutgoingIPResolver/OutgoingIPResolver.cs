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
            if (ipAddress.Equals(IPAddress.Any))
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