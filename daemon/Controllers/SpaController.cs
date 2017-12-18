using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    [AllowAnonymous]
    public class SpaController : BaseController
    {
        public SpaController(IOptionsSnapshot<AppConfig> appConfig, ILogger<SpaController> logger,
            IDbConnection db) : base(appConfig, logger, db)
        {
            
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}