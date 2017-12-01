using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public class ExceptionHandler : IMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            
        }
}