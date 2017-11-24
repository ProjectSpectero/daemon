using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
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
        private readonly IEnumerable<IPAddress> _localAddresses = Utility.GetLocalIPs();
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
            InitiateServices();
        }


        public bool Process(string name, string action)
        {
            Type type = Utility.GetServiceType(name);

            var config = _serviceConfigManager.Generate(type);
            var service = GetOrCreateService(type);

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

        public IServiceConfig GetServiceConfig<T>() where T : new()
        {
            return null;
        }

        public IService GetOrCreateService (Type type)
        {
            if (_services.ContainsKey(type))
                return _services[type];
            else
            {
                _logger.LogError("SCORG: Requested service of type " + type + " was not found. Perhaps initialization failed?");
                return null;
            }
        }

        private void InitiateServices()
        {
            var type = typeof(IService);
            var implementers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p))
                .Where(p => p != type) // Skip `IService` itself, it cannot be activated
                .ToArray(); 


            _logger.LogDebug("IS: Found " + implementers.Length + " services to activate.");

            foreach (var serviceType in implementers)
            {
                _logger.LogDebug("IS: Processing activation request for " + serviceType);
                var service = (IService) Activator.CreateInstance(serviceType, _appConfig, _logger, _db, _authenticator,
                    _localNetworks, _localAddresses, _statistician);
                _logger.LogDebug("IS: Activation succeeded for " + serviceType);
                _services.TryAdd(serviceType, service);
            }
            _logger.LogDebug("IS: Successfully initialized " + _services.Count + " service(s).");
        }

        public ConcurrentDictionary<Type, IService> GetServices()
        {
            return _services;
        }
    }
}