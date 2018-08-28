using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;

namespace Spectero.daemon.Libraries.Services
{
    public abstract class BaseService : IService
    {
        internal readonly AppConfig _appConfig;
        internal readonly ILogger<IService> _logger;
        internal readonly IDbConnection _db;
        internal readonly IAuthenticator _authenticator;
        internal readonly IEnumerable<IPNetwork> _localNetworks;
        internal readonly IEnumerable<IPAddress> _localAddresses;

        public BaseService(IServiceProvider serviceProvider)
        {
            _appConfig = serviceProvider.GetRequiredService<IOptionsMonitor<AppConfig>>().CurrentValue;
            _db = serviceProvider.GetRequiredService<IDbConnection>();
            _authenticator = serviceProvider.GetRequiredService<IAuthenticator>();

            _logger = serviceProvider.GetRequiredService<ILogger<IService>>();
            
            _localNetworks = Utility.GetLocalRanges(_logger);
            _localAddresses = Utility.GetLocalIPs();
        }

        public BaseService()
        {
        }

        public abstract void Start(IEnumerable<IServiceConfig> serviceConfig = null);
        public abstract void ReStart(IEnumerable<IServiceConfig> serviceConfig = null);
        public abstract void Stop();
        public abstract void Reload(IEnumerable<IServiceConfig> serviceConfig);
        public abstract void LogState(string caller);
        public abstract ServiceState GetState();
        public abstract IEnumerable<IServiceConfig> GetConfig();
        public abstract void SetConfig(IEnumerable<IServiceConfig> config, bool restartNeeded = false);
    }
}