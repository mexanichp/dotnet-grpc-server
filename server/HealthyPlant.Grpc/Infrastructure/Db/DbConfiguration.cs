using System;
using System.Text.RegularExpressions;
using HealthyPlant.Data;
using HealthyPlant.Grpc.Helpers;
using HealthyPlant.Grpc.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace HealthyPlant.Grpc.Infrastructure
{
    public class DbConfiguration
    {
        public static void ConfigureMongoDb()
        {
            var conventionPack = new ConventionPack();
            conventionPack.AddMemberMapConvention("LowerCaseMember",
                map => map.SetElementName(Regex.Replace(
                        map.MemberName,
                        "([a-z])([A-Z])",
                        "$1_$2",
                        RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(100))
                    .ToLowerInvariant()));

            ConventionRegistry.Register("LowerCase", conventionPack, type => true);
        }

        public static IMongoClient ConfigureSingletonClient(IServiceProvider provider)
        {
            var connectionString = provider.GetRequiredService<IOptions<PlantsDbSettings>>().Value.ConnectionString;
            var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            mongoSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe(CommandStartedHandler(scopeFactory));
                cb.Subscribe(CommandSucceededHandler(scopeFactory));
                cb.Subscribe(CommandFailedHandler(scopeFactory));
            };

            return new MongoClient(mongoSettings);
        }

        private const string CommandKey = "MongoCommand";
        private static Action<CommandStartedEvent> CommandStartedHandler(IServiceScopeFactory scopeFactory) => @event =>
        {
            using var spScope = scopeFactory.CreateScope();
            var sp = spScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestServices ?? spScope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<MongoClient>>();
            try
            {
                var diagnostics = sp.GetRequiredService<IDiagnosticContext>();
                diagnostics.Set(CommandKey, @event.Command.ToString());

                //TODO: remove this line
                using var logScope2 = logger.MongoDbSlowCommandSucceededEventScope(200, @event.Command.ToString());
                using var logScope = logger.MongoDbCommandEventScope(@event.RequestId);
                logger.MongoDbCommandStartedEvent(@event.CommandName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "CommandStartedEvent subscription failed.");
            }
        };

        private static Action<CommandSucceededEvent> CommandSucceededHandler(IServiceScopeFactory scopeFactory) => @event =>
        {
            using var spScope = scopeFactory.CreateScope();
            var sp = spScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestServices ?? spScope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<MongoClient>>();
            try
            {
                var diagnostics = sp.GetRequiredService<IDiagnosticContext>();
                var loggerDefaults = sp.GetRequiredService<IOptionsSnapshot<LoggerOptions>>()
                    .Value;

                using var logScope = logger.MongoDbCommandSucceededEventScope(@event.RequestId, @event.Reply.ToRootDictionary());
                var duration = @event.Duration.TotalMilliseconds;
                if (duration > loggerDefaults.ExpectedDbDurationLessThanMs)
                {
                    // TODO: fix scoped service doesn't exist when job is running
                    diagnostics.Pop(CommandKey, out var command);

                    using var logScope2 = logger.MongoDbSlowCommandSucceededEventScope(duration, command);
                    logger.MongoDbSlowCommandSucceededEvent(@event.CommandName);
                    return;
                }

                diagnostics.Remove(CommandKey);
                logger.MongoDbCommandSucceededEvent(@event.CommandName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "CommandSucceededEvent subscription failed.");
            }
        };

        private static Action<CommandFailedEvent> CommandFailedHandler(IServiceScopeFactory scopeFactory) => @event =>
        {
            using var spScope = scopeFactory.CreateScope();
            var sp = spScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestServices ?? spScope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<MongoClient>>();
            try
            {
                var diagnostics = sp.GetRequiredService<IDiagnosticContext>();
                diagnostics.Pop(CommandKey, out var command);

                using var logScope = logger.MongoDbCommandFailedEventScope(@event.RequestId, command);
                logger.MongoDbCommandFailedEvent(@event.Failure, @event.CommandName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "CommandFailedEvent subscription failed.");
            }
        };
    }
}