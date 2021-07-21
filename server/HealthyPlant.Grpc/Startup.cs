using HealthyPlant.Grpc.Infrastructure;
using HealthyPlant.Grpc.Jobs;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // gRPC configuration
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcLogInterceptor>();
            });
            services.AddGrpcReflection();
            services.AddHttpContextAccessor();

            // Mediator configuration.
            services.AddMediatR(typeof(Startup).Assembly);

            // Infrastructure configuration.
            services.RegisterOptions(Configuration);
            services.RegisterServices(Configuration);

            MappingConfig.RegisterMappings();

            // Database configuration.
            DbConfiguration.ConfigureMongoDb();

            // Authentication configuration.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, Configuration.GetSection(nameof(JwtBearerOptions)));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();
            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation("Configure {Environemnt}", env.EnvironmentName);

            if (env.IsDevelopment())
            {
                DevMigrations.PerformMigrationsAsync(app.ApplicationServices).GetAwaiter().GetResult();
            }

            app.UseAppLog();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<DataService>();

                if (env.IsDevelopment())
                {
                    endpoints.MapGrpcReflectionService();
                }

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });

                endpoints.MapPost("/notify-plants", async context =>
                {
                    if (!context.Request.Headers.TryGetValue("authorizationjob", out var key) || key != Configuration["JobsApiKey"])
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthenticated.");
                        return;
                    }

                    var serviceProvider = context.RequestServices;
                    var lg = serviceProvider.GetRequiredService<ILogger<PlantActionNotifier>>();
                    lg.LogInformation("Notify Plant action started.");

                    var notifier = new PlantActionNotifier(serviceProvider, lg);
                    await notifier.NotifyAsync();

                    lg.LogInformation("Notify Plant action finished.");
                    await context.Response.WriteAsync("Done");
                });
            });
        }
    }
}
