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

        private readonly IDbConnectionFactory _dbConnectionFactory;
        
        protected readonly IDbConnection Db;
        
        public BaseController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger, IDbConnectionFactory dbConnectionFactory)
        {
            AppConfig = appConfig.Value;
            Logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
            Db = dbConnectionFactory.Open();
        }
    }
}