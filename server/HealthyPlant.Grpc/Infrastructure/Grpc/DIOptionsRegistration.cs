using HealthyPlant.Data;
using HealthyPlant.Grpc.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class DIOptionsRegistration
    {
        public static IServiceCollection RegisterOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<PlantsDbSettings>()
                .Bind(configuration.GetSection("DbSettings"))
                .Validate(s => !string.IsNullOrWhiteSpace(s.ConnectionString)
                               && !string.IsNullOrWhiteSpace(s.DatabaseName)
                               && !string.IsNullOrWhiteSpace(s.UsersCollectionName)
                               && !string.IsNullOrWhiteSpace(s.OldHistoryCollectionName));
            services
                .AddSingleton(sp => sp.GetRequiredService<IOptions<PlantsDbSettings>>().Value);

            services
                .AddOptions<LoggerOptions>()
                .Bind(configuration.GetSection("LoggerOptions"))
                .Validate(s => s.ExpectElapsedLessThanMs > 0d
                               && s.ExpectedDbDurationLessThanMs > 0d);

            return services;
        }
    }
}