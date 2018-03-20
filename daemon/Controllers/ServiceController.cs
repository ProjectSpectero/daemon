﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;
using Utility = Spectero.daemon.Libraries.Core.Utility;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ServiceController : BaseController
    {
        private readonly IServiceManager _serviceManager;
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IOutgoingIPResolver _outgoingIpResolver;

        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IServiceConfigManager serviceConfigManager, IStatistician statistician,
            IOutgoingIPResolver outgoingIpResolver)
            : base(appConfig, logger, db)
        {
            _serviceManager = serviceManager;
            _serviceConfigManager = serviceConfigManager;
            _outgoingIpResolver = outgoingIpResolver;
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
            if (Defaults.ValidServices.Any(s => name == s) &&
                Defaults.ValidActions.Any(s => task == s))
            {
                if (task.Equals("config"))
                {
                    var type = Utility.GetServiceType(name);
                    var config = _serviceConfigManager.Generate(type);
                    _response.Result = config;
                    return Ok(_response);
                }

                var message = _serviceManager.Process(name, task, out var error);

                if (error != null)
                {
                    _response.Errors.Add(message, error);
                    return StatusCode(500, _response);
                }                 

                _response.Message = message;
                return Ok(_response);
            }

            _response.Errors.Add(Errors.INVALID_SERVICE_OR_ACTION_ATTEMPT, "");
            return BadRequest(_response);
        }

        [HttpPut("HTTPProxy/config", Name = "HandleHTTPProxyConfigUpdate")]
        public async Task<IActionResult> HandleHttpProxyConfigUpdate([FromBody] HTTPConfig config)
        {
            if (! ModelState.IsValid || config.listeners.IsNullOrEmpty())
            {
                // ModelState takes care of checking if the field map succeeded. This means mode | allowed.d | banned.d do not need manual checking
                _response.Errors.Add(Errors.MISSING_BODY, "");
                return BadRequest(_response);
            }
                
            var currentConfig = (HTTPConfig) _serviceConfigManager.Generate(Utility.GetServiceType("HTTPProxy")).First();

            var localAvailableIPs = Utility.GetLocalIPs();
            var availableIPs = localAvailableIPs as IPAddress[] ?? localAvailableIPs.ToArray();

            // Check if all listeners are valid
            foreach (var listener in config.listeners)
            {
                if (IPAddress.TryParse(listener.Item1, out var holder))
                {                   
                    if (AppConfig.BindToUnbound || availableIPs.Contains(holder) || holder.Equals(IPAddress.Any))
                        continue;
                    Logger.LogError("CCHH: Invalid listener request for " + holder + " found.");
                    _response.Errors.Add(Errors.INVALID_IP_AS_LISTENER_REQUEST, "");
                    break;
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
                // If listener config changed AND service is running, the service needs restarting as it can't be adjusted without the sockets being re-initialized.         
                var service = _serviceManager.GetService(typeof(HTTPProxy));
                var restartNeeded = !config.listeners.SequenceEqual(currentConfig.listeners) && service.GetState() == ServiceState.Running;

                service.SetConfig(new List<IServiceConfig> { config }, restartNeeded); //Update the running config, listener config will not apply until a full system restart is made. There's a bug here.

                if (restartNeeded)
                    _response.Message = Messages.SERVICE_RESTART_NEEDED;
            }

            // If we get to this point, it means the candidate config was valid and should be committed into the DB.

            var dbConfig = await Db.SingleAsync<Configuration>(x => x.Key == ConfigKeys.HttpConfig);
            dbConfig.Value = JsonConvert.SerializeObject(config);
            await Db.UpdateAsync(dbConfig);

            _response.Result = config;
            return Ok(_response);
        }

        [HttpGet("ips", Name = "GetLocalIPs")]
        public async Task<IActionResult> GetLocalIPs()
        {
            var ips = Utility.GetLocalIPs(AppConfig.IgnoreRFC1918);
            var addresses = ips.Select(ip => ip.ToString()).ToList();

            if (addresses.Count == 0)
            {
                var ip = await _outgoingIpResolver.Resolve();
                addresses.Add(ip.ToString());
            }
               
            _response.Result = addresses;
            return Ok(_response);
        }
    }
}