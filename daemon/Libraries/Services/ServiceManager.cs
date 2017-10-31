using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;

namespace Spectero.daemon.Libraries.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly AppConfig _appConfig;
        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks = Utility.GetLocalRanges();
        private readonly ILogger<ServiceManager> _logger;
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly ConcurrentDictionary<Type, IService> _services = new ConcurrentDictionary<Type, IService>();
        private readonly IStatistician _statistician;


        public ServiceManager(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IStatistician statistician, IServiceConfigManager serviceConfigManager)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _statistician = statistician;
            _serviceConfigManager = serviceConfigManager;
        }

        public bool Process(string name, string action)
        {
            IService service;
            IServiceConfig config;

            switch (name)
            {
                case "proxy":
                    config = (HTTPConfig) _serviceConfigManager.Generate<HTTPProxy.HTTPProxy>();
                    service = GetOrCreateService<HTTPProxy.HTTPProxy>();
                    break;
                case "vpn":
                    config = (OpenVPNConfig) _serviceConfigManager.Generate<OpenVPN.OpenVPN>();
                    service = GetOrCreateService<OpenVPN.OpenVPN>();
                    break;
                case "ssh":
                    config = null;
                    service = null;
                    break;
                default:
                    throw new EInvalidArguments();
            }

            if (service == null || config == null)
            {
                _logger.LogError("NAP: Resolved service or its config was null when processing name -> " + name + ", action -> " + action);
                return false;
            }                

            switch (action)
            {
                case "start":
                    service.Start(config);
                    break;
                case "stop":
                    service.Stop();
                    break;
                case "restart":
                    service.ReStart(config);
                    break;
            }

            return true;
        }

        private IService GetOrCreateService<T>() where T : new()
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
                return _services[type];
            var service = (IService) Activator.CreateInstance(type, _appConfig, _logger, _db, _authenticator,
                _localNetworks, _statistician);
            _services.TryAdd(type, service);
            return service;
        }

        public ConcurrentDictionary<Type, IService> GetServices()
        {
            return _services;
        }
    }
}