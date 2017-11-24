using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(UserController))]
    public class UserController : BaseController
    {
        public UserController(IOptionsSnapshot<AppConfig> appConfig, ILogger<UserController> logger,
            IDbConnection db)
            : base(appConfig, logger, db)
        {

        }
        // GET
        public IActionResult Index()
        {
            return Ok();
        }
    }
}