using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.HTTP;
using Spectero.daemon.Models;

namespace Spectero.daemon.HTTP.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppConfig AppConfig;
        protected readonly IDbConnection Db;
        protected readonly ILogger<BaseController> Logger;
        protected readonly APIResponse _response;
        private User _currentUser;

        public BaseController(IOptionsSnapshot<AppConfig> appConfig, ILogger<BaseController> logger,
            IDbConnection db)
        {
            AppConfig = appConfig.Value;
            Logger = logger;
            Db = db;
            _response = APIResponse.Create(null, new Dictionary<string, object> (), null);
        }

        protected bool HasErrors()
        {
            return _response.Errors.Count > 0;
        }

        private IEnumerable<Claim> GetClaims()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            return identity?.Claims;
        }

        private Claim GetClaim(string type)
        {
            return GetClaims().FirstOrDefault(x => x.Type == type);
        }

        protected User CurrentUser()
        {
            if (_currentUser != null)
                return _currentUser;

            var userData = GetClaim(ClaimTypes.UserData)?.Value;
            var user = JsonConvert.DeserializeObject<User>(userData);

            if (user == null || user.Id == 0) return null;

            // Some caching would be good here, but this really doesn't service enough requests to justify it.
            _currentUser = Db.SingleById<User>(user.Id);
            return _currentUser;

        }

        protected async Task<Configuration> CreateOrUpdateConfig(string key, string value)
        {
            return await ConfigUtils.CreateOrUpdateConfig(Db, key, value);
        }

        protected async Task<Configuration> GetConfig(string key)
        {
            return await ConfigUtils.GetConfig(Db, key);
        }

        protected async Task<int> DeleteConfigIfExists(string key)
        {
            return await ConfigUtils.DeleteConfigIfExists(Db, key);
        }
    }
}