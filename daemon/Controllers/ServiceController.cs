using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Templates;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;
using Utility = Spectero.daemon.Libraries.Core.Utility;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    public class ServiceController : BaseController
    {
        private readonly IServiceManager _serviceManager;
        private readonly string[] _validActions = {"start", "stop", "restart", "config"};

        private readonly string[] _validServices = {"HTTPProxy", "OpenVPN", "ShadowSOCKS", "SSHTunnel"};

        private readonly IServiceConfigManager _serviceConfigManager;

        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IServiceConfigManager serviceConfigManager, IStatistician statistician)
            : base(appConfig, logger, db)
        {
            _serviceManager = serviceManager;
            _serviceConfigManager = serviceConfigManager;
        }

        [HttpGet("", Name = "IndexServices")]
        public IActionResult Index()
        {
            var services = _serviceManager.GetServices();
            var ret = new Dictionary<string, string>();
            foreach (var service in services)
            {
                ret.Add(service.Value.GetType().Name, service.Value.GetState().ToString());
            }
            _response.Result = ret;
            return Ok(_response);
        }

        [HttpGet("{name}/{task}", Name = "ManageServices")]
        public IActionResult Manage(string name, string task)
        {
            if (_validServices.Any(s => name == s) &&
                _validActions.Any(s => task == s))
            {
                if (task.Equals("config"))
                {
                    var type = Utility.GetServiceType(name);
                    var config = _serviceConfigManager.Generate(type);
                    _response.Result = config;
                    return Ok(_response);
                }

                _serviceManager.Process(name, task);
                _response.Message = Messages.SERVICE_STARTED;
                return Ok(_response);
            }
            throw new EInvalidRequest();
        }

        [HttpPut("HTTPProxy/config", Name = "HandleHTTPProxyConfigUpdate")]
        public async Task<IActionResult> HandleHttpProxyConfigUpdate([FromBody] HTTPConfig config)
        {
            if (! ModelState.IsValid || config.listeners.IsNullOrEmpty())
            {
                // ModelState takes care of checking if the field map succeeded. This means mode | allowed.d | banned.d do not need manual checking
                _response.Errors.Add(Errors.MISSING_BODY);
                return BadRequest(_response);
            }
                
            var currentConfig = (HTTPConfig) _serviceConfigManager.Generate(Utility.GetServiceType("HTTPProxy"));

            var localAvailableIPs = Utility.GetLocalIPs();
            var availableIPs = localAvailableIPs as IPAddress[] ?? localAvailableIPs.ToArray();

            // Check if all listeners are valid
            foreach (var listener in config.listeners)
            {
                if (IPAddress.TryParse(listener.Item1, out var holder))
                {                   
                    if (availableIPs.Contains(holder) || holder.Equals(IPAddress.Any))
                        continue;
                    Logger.LogError("CCHH: Invalid listener request for " + holder + " found.");
                    _response.Errors.Add(Errors.INVALID_IP_AS_LISTENER_REQUEST);
                }
            }

            if (HasErrors())              
                return BadRequest(_response);


            if (config.listeners != currentConfig.listeners ||
                config.proxyMode != currentConfig.proxyMode ||
                config.allowedDomains != currentConfig.allowedDomains ||
                config.bannedDomains != currentConfig.bannedDomains)
            {
                // A difference was found between the running config and the candidate config
                // If listener config changed, the service needs restarting as it can't be adjusted without the sockets being re-initialized.
                var restartNeeded = ! config.listeners.SequenceEqual(currentConfig.listeners);          
                var service = _serviceManager.GetService(typeof(HTTPProxy));

                service.SetConfig(config, restartNeeded); //Update the running config, listener config will not apply until a full system restart is made. There's a bug here.

                if (restartNeeded)
                    _response.Message = Messages.SERVICE_RESTART_NEEDED;
            }

            // If we get to this point, it means the candidate config was valid and should be committed into the DB.

            var dbConfig = await Db.SingleAsync<Configuration>(x => x.Key == ConfigKeys.HttpConfig);
            if (dbConfig != null)
            {
                dbConfig.Value = JsonConvert.SerializeObject(config);
                await Db.UpdateAsync(dbConfig);

                _response.Result = config;
                return Ok(_response);
            }
            _response.Errors.Add(Errors.STORED_CONFIG_WAS_NULL);
            return StatusCode(500, _response);
        }

        [HttpGet("ips", Name = "GetLocalIPs")]
        public IActionResult GetLocalIPs()
        {
            var ips = Utility.GetLocalIPs();
            var addresses = new List<string>();
            foreach (var ip in ips)
            {
                addresses.Add(ip.ToString());
            }
            _response.Result = addresses;
            return Ok(_response);
        }
    }
}