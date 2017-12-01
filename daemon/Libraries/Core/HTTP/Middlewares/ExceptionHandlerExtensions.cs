using Microsoft.AspNetCore.Builder;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public static class ExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseSpecteroErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandler>();
        }
    }
}