using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class AddCORS
    {
        // app.UseCors seems to have issues, deployed as a quick and dirty hack.
        private readonly RequestDelegate _next;

        public AddCORS(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            httpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            httpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Date, X-Api-Version, X-File-Name");
            httpContext.Response.Headers.Add("Access-Control-Allow-Methods", "POST,GET,PUT,PATCH,DELETE,OPTIONS");
            return _next(httpContext);
        }
    }
}