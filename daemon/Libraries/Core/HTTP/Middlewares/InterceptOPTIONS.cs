using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class InterceptOptions
    {
        // app.UseCors seems to have issues, deployed as a quick and dirty hack.
        private readonly RequestDelegate _next;

        public InterceptOptions(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Method == "OPTIONS") // Horrible hack
            {
                httpContext.Response.StatusCode = 200;
                await httpContext.Response.WriteAsync("");
            }                          
        }
    }
}