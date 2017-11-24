using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class ResponseWrapper : IMiddleware
    {
        /*
         * Heavily adapted from a Stack Overflow example
         * Credit: https://stackoverflow.com/a/41398338
         */
        private readonly RequestDelegate _next;

        public ResponseWrapper(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var currentBody = context.Response.Body;

            using (var memoryStream = new MemoryStream())
            {
                // Translation logic from the interim response to the final formatted response goes here
                //set the current response to the memorystream.
                context.Response.Body = memoryStream;

                await _next(context);

                //reset the body 
                context.Response.Body = currentBody;
                memoryStream.Seek(0, SeekOrigin.Begin);

                var readToEnd = new StreamReader(memoryStream).ReadToEnd();
                await context.Response.WriteAsync(JsonConvert.SerializeObject(readToEnd));
            }
        }
    }
}