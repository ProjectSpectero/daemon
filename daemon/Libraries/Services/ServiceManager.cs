using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Data;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services.HTTPProxy;

namespace Spectero.daemon.Libraries.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private readonly AppConfig _appConfig;
        private readonly ILogger<ServiceManager> _logger;

        public ServiceManager(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceManager> logger, IDbConnectionFactory dbConnectionFactory)
        {
            _appConfig = appConfig.CurrentValue;
            _logger = logger;
        }

        public bool Process (string name, string action)
        {
            bool returnValue = false;
            
            switch (name)
            {
                case "proxy":
                    var config = (HTTPConfig) ServiceConfigManager.Generate<HTTPProxy.HTTPProxy>();
                    var service = GetOrCreateService<HTTPProxy.HTTPProxy>();
                    switch (action)
                    {
                       case "start":
                           service.Start(config);
                           returnValue = true;
                           break;
                       case "stop":
                            break;
                       case "restart":
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
                var service = (IService) Activator.CreateInstance(type, _appConfig);
                _services.Add(type, service);
                return service;
            }
        }
    }
}