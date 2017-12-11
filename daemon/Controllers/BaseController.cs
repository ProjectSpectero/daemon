using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.HTTP;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
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
            _response = APIResponse.Create(null, new List<object>(), null);
        }

        protected bool HasErrors()
        {
            return _response.Errors.Count > 0;
        }

        protected IEnumerable<Claim> GetClaims()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            return identity?.Claims;
        }

        protected Claim GetClaim(string type)
        {
            return GetClaims().FirstOrDefault(x => x.Type == type);
        }

        protected User CurrentUser()
        {
            if (_currentUser != null)
                return _currentUser;

            string userIdString = GetClaim(ClaimTypes.UserData)?.Value;
            if (int.TryParse(userIdString, out var id))
            {
                _currentUser = Db.SingleById<User>(id);
                return _currentUser;
            }
            return null;
        }
    }
}