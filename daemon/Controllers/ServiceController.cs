using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    public class ServiceController : BaseController
    {
        private readonly IServiceManager _serviceManager;
        private readonly string[] _validActions = {"start", "stop", "restart"};

        private readonly string[] _validServices = {"HTTPProxy", "OpenVPN", "ShadowSOCKS", "SSHTunnel"};

        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IStatistician statistician)
            : base(appConfig, logger, db)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet("", Name = "IndexServices")]
        public async Task<IActionResult> Index()
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
        public async Task<IActionResult> Manage(string name, string task)
        {
            Logger.LogDebug("Service manager n -> " + name + ", a -> " + task);

            if (_validServices.Any(s => name == s) &&
                _validActions.Any(s => task == s))
            {
                _serviceManager.Process(name, task);
                _response.Message = Messages.SERVICE_STARTED;
                return Ok(_response);
            }
            throw new EInvalidRequest();
        }
    }
}