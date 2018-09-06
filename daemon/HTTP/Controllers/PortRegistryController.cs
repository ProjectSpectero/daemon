using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.PortRegistry;

namespace Spectero.daemon.HTTP.Controllers
{
    [Route("v1/port-registry")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(PortRegistryController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PortRegistryController : BaseController
    {
        private readonly IPortRegistry _portRegistry;
        
        public PortRegistryController(ILogger<PortRegistryController> logger, IPortRegistry portRegistry,
            IOptionsSnapshot<AppConfig> appConfig, IDbConnection db) : base(appConfig, logger, db)
        {
            _portRegistry = portRegistry;
        }
        
        // GET
        public IActionResult Index()
        {
            _response.Result = _portRegistry.GetAllAllocations();

            return Ok(_response);
        }
    }
}