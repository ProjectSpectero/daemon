using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;

namespace Spectero.daemon.HTTP.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class SystemController : BaseController
    {
        private readonly IApplicationLifetime _applicationLifetime;
        
        public SystemController(IOptionsSnapshot<AppConfig> appConfig, ILogger<BaseController> logger,
            IDbConnection db, IApplicationLifetime applicationLifetime) : base(appConfig, logger, db)
        {
            _applicationLifetime = applicationLifetime;
        }

        [HttpPost("shutdown", Name = "ShutdownApplication")]
        public IActionResult Shutdown()
        {
            _applicationLifetime.StopApplication();

            _response.Message = Messages.APPLICATION_STATE_TOGGLED;

            return Ok(_response);
        }
    }
}