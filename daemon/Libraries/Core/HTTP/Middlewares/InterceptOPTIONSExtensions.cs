using Microsoft.AspNetCore.Builder;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public static class InterceptOptionsExtensions
    {
        public static IApplicationBuilder UseInterceptOptions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<InterceptOptions>();
        }
    }
}