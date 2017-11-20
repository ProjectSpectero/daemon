using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class AddRequestIdHeader : IMiddleware
    {
        /*
         * Heavily adapted from an example by Tugberk Ugurlu 
         * Credit: http://www.tugberkugurlu.com/archive/asp-net-5-and-log-correlation-by-request-id
         */

        private readonly RequestDelegate _next;

        public AddRequestIdHeader(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestIdFeature = context.Features.Get<IHttpRequestIdentifierFeature>();
            if (requestIdFeature?.TraceIdentifier != null)
            {
                context.Response.Headers["RequestId"] = requestIdFeature.TraceIdentifier;
            }

            await _next(context);
        }
    }
}