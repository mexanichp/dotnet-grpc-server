using Microsoft.AspNetCore.Builder;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseAppLog(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AppLogMiddleware>();
        }
    }
}