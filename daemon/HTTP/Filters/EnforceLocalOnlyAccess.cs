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
                    _logger.LogDebug($"Blocked access to {httpContext.Request.Path} from {ipString} because it is denoted localhost only.");
                    
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