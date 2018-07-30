using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorLight;
using Spectero.daemon.HTTP.Filters;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.HTTP.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ServiceFilter(typeof(EnforceLocalOnlyAccess))]
    [Route("v1/[controller]")]
    public class DebugController : BaseController
    {

        private readonly IAuthenticator _authenticator;
        private readonly IDbConnection _db;
        private readonly IEnumerable<IPNetwork> _localNetworks = Utility.GetLocalRanges();
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IStatistician _statistician;
        private readonly IRazorLightEngine _engine;
        private readonly IIdentityProvider _identity;
        private readonly IProcessRunner _processRunner;

        public DebugController(IOptionsSnapshot<AppConfig> appConfig, ILogger<DebugController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IServiceConfigManager serviceConfigManager, IStatistician statistician,
            IIdentityProvider identityProvider, IRazorLightEngine engine,
            IProcessRunner processRunner)
            : base(appConfig, logger, db)
        {
            _engine = engine;
            _identity = identityProvider;
            _serviceConfigManager = serviceConfigManager;
            _processRunner = processRunner;

        }
        
        
        [HttpGet("", Name = "Index")]
        public async Task<IActionResult> Index()
        {
            return Ok(_response);
        }
        
        [HttpGet("errors/{type}", Name = "DebugErrorMarshaling")]
        public async Task<IActionResult> DebugErrors(string type)
        {
            switch (type)
            {
                    case "internal":
                        throw new InternalError("Testing internal errors...");
                    
                    case "disclosable":
                        throw new DisclosableError();
                    
                    case "validation":
                        throw new ValidationError();
                    
                    default:
                        throw new DisclosableError();
            }    
       }

        [HttpGet("network/{type?}", Name = "DebugNetworkDiscovery")]
        public async Task<IActionResult> DebugNetworkDiscovery(string type = "")
        {
            switch (type)
            {
                    case "ips":
                        _response.Result = Utility.GetLocalIPs().Select(x => x.ToString()).ToList();
                        break;
                        
                    default:
                        _response.Result = Utility.GetLocalRanges(Logger).Select(x => x.ToString()).ToList();
                        break;
            }

            return Ok(_response);
        }
    }
}