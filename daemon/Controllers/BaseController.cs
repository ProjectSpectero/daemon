using System.Collections.Generic;
using System.Data;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.HTTP;

namespace Spectero.daemon.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppConfig AppConfig;
        protected readonly IDbConnection Db;
        protected readonly ILogger<BaseController> Logger;
        protected readonly APIResponse _response;

        public BaseController(IOptionsSnapshot<AppConfig> appConfig, ILogger<BaseController> logger,
            IDbConnection db)
        {
            AppConfig = appConfig.Value;
            Logger = logger;
            Db = db;
            _response = APIResponse.Create(null, new List<object>(), null);
        }

        public bool HasErrors()
        {
            return _response.Errors.Count > 0;
        }
    }
}