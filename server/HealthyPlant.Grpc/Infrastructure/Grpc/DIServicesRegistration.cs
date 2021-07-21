using HealthyPlant.Data;
using HealthyPlant.Grpc.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class DIServicesRegistration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(DbConfiguration.ConfigureSingletonClient);

            services.AddScoped<IMongoRepository, MongoRepository>();

            services.AddScoped<IDiagnosticContext, DiagnosticContext>();

            services.AddSingleton<IAppFirebaseMessaging, AppFirebaseMessaging>();

            return services;
        }
    }
}