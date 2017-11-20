using Microsoft.AspNetCore.Builder;

namespace Spectero.daemon.Libraries.Core.HTTP.Middlewares
{
    public static class AddRequestIdHeaderExtensions
    {
        public static IApplicationBuilder UseAddRequestIdHeader(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AddRequestIdHeader>();
        }
    }
}