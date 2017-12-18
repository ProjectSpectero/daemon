using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Models;

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
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();
            var webRootPath = AppConfig.WebRoot;
            var spaFileName = AppConfig.SpaFileName;

            var qualifiedPath = Path.Combine(currentDirectory, webRootPath, spaFileName);
            var viewBag = new Configuration();

            using (StreamReader streamReader = new StreamReader(qualifiedPath))
            {
                viewBag.Value = streamReader.ReadToEnd();
            }

            return View(viewBag);
        }
    }
}