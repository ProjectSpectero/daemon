using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Data;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services.HTTPProxy;

namespace Spectero.daemon.Libraries.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly ConcurrentDictionary<Type, IService> _services = new ConcurrentDictionary<Type, IService>();
        private readonly AppConfig _appConfig;
        private readonly ILogger<ServiceManager> _logger;
        private readonly IDbConnection _db;
        private readonly IAuthenticator _authenticator;
        private readonly IEnumerable<IPNetwork> _localNetworks = Utility.GetLocalRanges();
        private readonly IStatistician _statistician;

        public ServiceManager(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger,
            IDbConnection db, IAuthenticator authenticator,
            IStatistician statistician)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
            _db = db;
            _authenticator = authenticator;
            _statistician = statistician;
        }

        public bool Process (string name, string action)
        {
            bool returnValue = true;
            
            switch (name)
            {
                case "proxy":
                    var config = (HTTPConfig) ServiceConfigManager.Generate<HTTPProxy.HTTPProxy>();
                    var service = GetOrCreateService<HTTPProxy.HTTPProxy>();
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
                    break;
                case "vpn":
                    break;
                case "ssh":
                    break;
                default:
                    throw new EInvalidArguments();
            }
            
            return returnValue;
        }

        private IService GetOrCreateService<T> () where T: new ()
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
                return _services[type];
            else
            {
                var service = (IService) Activator.CreateInstance(type, _appConfig, _logger, _db, _authenticator, _localNetworks, _statistician);
                _services.TryAdd(type, service);
                return service;
            }
        }

        public ConcurrentDictionary<Type, IService> GetServices ()
        {
            return _services;
        }
    }
}