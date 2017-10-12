using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    [Route("v1/[controller]")]
    public class DocumentationController : BaseController
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiExplorer;


        public DocumentationController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IApiDescriptionGroupCollectionProvider apiExplorer)
            : base(appConfig, logger, db)
        {
            _apiExplorer = apiExplorer;
        }

        public IActionResult Index()
        {
            return View(_apiExplorer);
        }
    }
}