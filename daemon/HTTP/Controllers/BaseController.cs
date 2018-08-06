/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.HTTP;
using Spectero.daemon.Libraries.Errors;
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
            user.LastLoginDate = DateTime.UtcNow;

            if (user == null || user.AuthKey.IsNullOrEmpty())
                throw new InternalError("JWT token did NOT contain a valid user object!");

            _currentUser = user;
            
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