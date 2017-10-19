using System.Collections.Generic;
using System.Data;
using System.Net;
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
        private readonly ILogger<ServiceManager> _logger;
        private readonly IStatistician _statistician;
        private OpenVPNConfig _vpnConfig;
        private ServiceState State = ServiceState.Halted;

        public OpenVPN()
        {
            
        }

        public OpenVPN(AppConfig appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IEnumerable<IPNetwork> localNetworks, IStatistician statistician)
        {
            _appConfig = appConfig;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _localNetworks = localNetworks;
            _statistician = statistician;
        }

        public void Start(IServiceConfig serviceConfig)
        {
            _vpnConfig = (OpenVPNConfig) serviceConfig;
        }

        public void ReStart(IServiceConfig serviceConfig)
        {
            _vpnConfig = (OpenVPNConfig)serviceConfig;
        }

        public void Reload(IServiceConfig serviceConfig)
        {
            _vpnConfig = (OpenVPNConfig)serviceConfig;
        }

        public void Stop()
        {
            
        }
        public ServiceState GetState()
        {
            return State;
        }

        public void LogState(string caller)
        {
            
        }
    }
}