using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppConfig AppConfig;
        protected readonly IDbConnection Db;
        protected readonly ILogger<BaseController> Logger;

        public BaseController(IOptionsSnapshot<AppConfig> appConfig, ILogger<BaseController> logger,
            IDbConnection db)
        {
            AppConfig = appConfig.Value;
            Logger = logger;
            Db = db;
        }
    }
}