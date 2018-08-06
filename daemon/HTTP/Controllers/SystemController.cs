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
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;

namespace Spectero.daemon.HTTP.Controllers
{
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class SystemController : BaseController
    {
        private readonly IApplicationLifetime _applicationLifetime;
        
        public SystemController(IOptionsSnapshot<AppConfig> appConfig, ILogger<BaseController> logger,
            IDbConnection db, IApplicationLifetime applicationLifetime) : base(appConfig, logger, db)
        {
            _applicationLifetime = applicationLifetime;
        }

        [HttpPost("shutdown", Name = "ShutdownApplication")]
        public IActionResult Shutdown()
        {
            _applicationLifetime.StopApplication();

            _response.Message = Messages.APPLICATION_STATE_TOGGLED;

            return Ok(_response);
        }
    }
}