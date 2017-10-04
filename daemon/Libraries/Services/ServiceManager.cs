using System.Collections.Generic;
using System.Net;
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
        private AppConfig _appConfig;

        public ServiceManager(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger, IDbConnectionFactory dbConnectionFactory)
        {
            _appConfig = appConfig.Value;
        }

        public bool Process (string name, string action)
        {
            
            switch (name)
            {
                case "proxy":
                    HTTPConfig config = (HTTPConfig) ServiceConfigManager.Generate<HTTPProxy.HTTPProxy>();
                    switch (action)
                    {
                       case "start":
                           var proxy = new HTTPProxy.HTTPProxy(new AppConfig());
                           proxy.Start(config);
                           break;
                        case "stop":
                            break;
                        case "restart":
                            break;          
                    }
                    break;
                case "vpn":
                    break;
                case "ssh":
                    break;
                default:
                    throw new EInvalidArguments();
                    
            }
            
        }
    }
}