using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(CloudController))]
    public class CloudController : BaseController
    {
        public CloudController(IOptionsSnapshot<AppConfig> appConfig, ILogger<CloudController> logger,
            IDbConnection db)
            : base(appConfig, logger, db)
        {
            
        }

        [HttpGet("config", Name = "GetLocalSystemConfig")]
        public IActionResult GetConfig()
        {
            _response.Result = AppConfig;
            return Ok(_response);
        }
    }
}