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
        private IPAddress address;
        private readonly AppConfig _appConfig;

        public OutgoingIPResolver(IOptionsMonitor<AppConfig> configMonitor)
        {
            _appConfig = configMonitor.CurrentValue;
            address = null;
        }

        public async Task<IPAddress> Resolve()
        {
            if (address != null)
                return address;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Spectero Daemon");
            var ipAddressString = await client.GetStringAsync(_appConfig.DefaultOutgoingIPResolver);

            if (IPAddress.TryParse(ipAddressString, out var parsedAddress))
                address = parsedAddress;

            return address;
        }
    }
}