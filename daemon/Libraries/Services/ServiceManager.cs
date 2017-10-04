using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Data;
using Spectero.daemon.Controllers;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services.HTTPProxy;

namespace Spectero.daemon.Libraries.Services
{
    public class ServiceManager : IServiceManager
    {
        private Dictionary<string, Dictionary<IService, ServiceState>> _services = new Dictionary<string, Dictionary<IService, ServiceState>>();
        private readonly AppConfig _appConfig;

        public ServiceManager(IOptionsMonitor<AppConfig> appConfig, ILogger<ServiceController> logger, IDbConnectionFactory dbConnectionFactory)
        {
            _appConfig = appConfig.CurrentValue;
        }

        public bool Process (string name, string action)
        {
            bool returnValue = false;
            
            switch (name)
            {
                case "proxy":
                    HTTPConfig config = (HTTPConfig) ServiceConfigManager.Generate<HTTPProxy.HTTPProxy>();
                    switch (action)
                    {
                       case "start":
                           var proxy = new HTTPProxy.HTTPProxy(_appConfig);
                           proxy.Start(config);
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
    }
}