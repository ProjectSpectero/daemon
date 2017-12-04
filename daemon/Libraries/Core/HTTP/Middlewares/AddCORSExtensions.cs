using Microsoft.AspNetCore.Builder;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public static class AddCORSExtensions
    {
        public static IApplicationBuilder UseAddCORS(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AddCORS>();
        }
    }
}