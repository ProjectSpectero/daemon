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
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Spectero.daemon.Libraries.Core.Constants;

namespace Spectero.daemon.HTTP.Filters
{
    public class EnforceLocalOnlyAccess : FilterBase
    {
        private readonly ILogger<EnforceLocalOnlyAccess> _logger;

        public EnforceLocalOnlyAccess(ILogger<EnforceLocalOnlyAccess> logger)
        {
            _logger = logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var clientIP = httpContext.Connection.RemoteIpAddress;
              

            if (!IPAddress.IsLoopback(clientIP))
            {
                var ipString = clientIP.ToString();
                                
                
                // This is what happens when we truly listen to all interfaces, might as well add handling for it.
                // It's a packed (v4 padded to be 128 bits) v6 IP, happens when v4 req is made to v6 listening port.
                
                if (!ipString.Equals("::ffff:127.0.0.1"))
                {
                    _logger.LogWarning($"Blocked access to {httpContext.Request.Path} from {ipString} because it is denoted localhost only.");
                    
                    Response.Errors.Add(Errors.LOOPBACK_ACCESS_ONLY, ipString);
                    
                    var response = new ObjectResult(Response)
                    {
                        StatusCode = (int) HttpStatusCode.Forbidden
                    };

                    context.Result = response;
                }
            }
                        
            base.OnActionExecuting(context);
        }
    }
}