using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static partial class LoggerDefaults
    {
        // Events
        private static readonly EventId MongoDb = new EventId(27017, "MongoDB event.");

        // Messages
        private static readonly Action<ILogger, string, Exception?> _mongoDbCommandStartedEvent =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                MongoDb,
                "MongoDB started event {CommandName} fired."
            );

        private static readonly Action<ILogger, string, Exception?> _mongoDbCommandSucceededEvent =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                MongoDb,
                "MongoDB succeeded event {CommandName} fired."
            );

        private static readonly Action<ILogger, string, Exception?> _mongoDbSlowCommandSucceededEvent =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                MongoDb,
                "MongoDB succeeded SLOWLY event {CommandName} fired."
            );

        private static readonly Action<ILogger, string, Exception> _mongoDbCommandFailedEvent =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                MongoDb,
                "MongoDB FAILED event {CommandName} fired."
            );

        public static void MongoDbCommandStartedEvent(this ILogger logger, string commandName) => _mongoDbCommandStartedEvent(logger, commandName, null);
        public static void MongoDbCommandSucceededEvent(this ILogger logger, string commandName) => _mongoDbCommandSucceededEvent(logger, commandName, null);
        public static void MongoDbSlowCommandSucceededEvent(this ILogger logger, string commandName) => _mongoDbSlowCommandSucceededEvent(logger, commandName, null);
        public static void MongoDbCommandFailedEvent(this ILogger logger, Exception exception, string commandName) => _mongoDbCommandFailedEvent(logger, commandName, exception);

        // Scopes
        private static readonly Func<ILogger, int, IDisposable> _mongoDbCommandEventScope =
            LoggerMessage.DefineScope<int>("{RequestId}");

        private static readonly Func<ILogger, int, Dictionary<string, object>, IDisposable> _mongoDbCommandSucceededEventScope =
            LoggerMessage.DefineScope<int, Dictionary<string, object>>("{RequestId} {Reply}");

        private static readonly Func<ILogger, double, object, IDisposable> _mongoDbSlowCommandSucceededEventScope =
            LoggerMessage.DefineScope<double, object>("{Duration:0.0000} {Command}");

        private static readonly Func<ILogger, int, object, IDisposable> _mongoDbCommandFailedEventScope =
            LoggerMessage.DefineScope<int, object>("{RequestId} {Command}");

        public static IDisposable MongoDbCommandEventScope(this ILogger logger, int requestId) => _mongoDbCommandEventScope(logger, requestId);
        public static IDisposable MongoDbCommandSucceededEventScope(this ILogger logger, int requestId, Dictionary<string, object> reply) => _mongoDbCommandSucceededEventScope(logger, requestId, reply);
        public static IDisposable MongoDbSlowCommandSucceededEventScope(this ILogger logger, double duration, object command) => _mongoDbSlowCommandSucceededEventScope(logger, duration, command);
        public static IDisposable MongoDbCommandFailedEventScope(this ILogger logger, int requestId, object command) => _mongoDbCommandFailedEventScope(logger, requestId, command);
    }
}