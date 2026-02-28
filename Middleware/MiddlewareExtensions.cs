using Microsoft.AspNetCore.Builder;

namespace WholesaleOrderSystem.API.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
