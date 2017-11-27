using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
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