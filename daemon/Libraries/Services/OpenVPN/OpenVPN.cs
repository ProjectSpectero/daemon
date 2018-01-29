using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Statistics;

namespace Spectero.daemon.Libraries.Services.OpenVPN
{
    public class OpenVPN : IService
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks;
        private readonly IEnumerable<IPAddress> _localAddresses;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IStatistician _statistician;
        private readonly IMemoryCache _cache;
        private IEnumerable<OpenVPNConfig> _vpnConfig;
        private readonly ServiceState State = ServiceState.Halted;

        public OpenVPN()
        {
        }

        public OpenVPN(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IEnumerable<IPAddress> localAddresses,
            IStatistician statistician, IMemoryCache cache)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _statistician = statistician;
            _cache = cache;
            _localAddresses = localAddresses;
        }

        public void Start(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            _vpnConfig = serviceConfig as List<OpenVPNConfig>;
        }

        public void ReStart(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            _vpnConfig = serviceConfig as List<OpenVPNConfig>;
        }

        public void Reload(IEnumerable<IServiceConfig> serviceConfig = null)
        {
            _vpnConfig = serviceConfig as List<OpenVPNConfig>;
        }

        public void Stop()
        {
            throw new NotSupportedException();
        }

        public ServiceState GetState()
        {
            return State;
        }

        public void LogState(string caller)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<IServiceConfig> GetConfig()
        {
            return _vpnConfig;
        }

        public void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false)
        {
            _vpnConfig = config as List<OpenVPNConfig>;
        }
    }
}