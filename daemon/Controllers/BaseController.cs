using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppConfig AppConfig;
        protected readonly ILogger<BaseController> Logger;
        protected readonly IDbConnection Db;
        
        public BaseController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger, IDbConnection db)
        {
            AppConfig = appConfig.Value;
            Logger = logger;
            Db = db;
        }
    }
}