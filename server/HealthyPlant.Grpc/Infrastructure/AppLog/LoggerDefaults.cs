using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static partial class LoggerDefaults
    {
        private static readonly EventId Response = new EventId(5000, "Request finished.");

        private static readonly Action<ILogger, int, double, Exception?> _requestFinished = LoggerMessage.Define<int, double>(
            LogLevel.Information,
            Response,
            "HTTP responded {StatusCode} in {Elapsed:0.0000} ms"
        );

        private static readonly Action<ILogger, int, double, Exception?> _requestFinishedWithError = LoggerMessage.Define<int, double>(
            LogLevel.Error,
            Response,
            "HTTP responded with ERROR {StatusCode} in {Elapsed:0.0000} ms"
        );

        private static readonly Action<ILogger, int, double, Exception> _requestFinishedWithException = LoggerMessage.Define<int, double>(
            LogLevel.Error,
            Response,
            "HTTP responded with EXCEPTION {StatusCode} in {Elapsed:0.0000} ms"
        );


        private static readonly Action<ILogger, StatusCode, double, Exception?> _requestFinishedGrpc = LoggerMessage.Define<StatusCode, double>(
            LogLevel.Information,
            Response,
            "GRPC responded {StatusCode} in {Elapsed:0.0000} ms"
        );


        private static readonly Action<ILogger, StatusCode, string, double, Exception> _requestFinishedGrpcWithError = LoggerMessage.Define<StatusCode, string, double>(
            LogLevel.Error,
            Response,
            "GRPC responded with ERROR {StatusCode} - {Detail} in {Elapsed:0.0000} ms"
        );


        private static readonly Action<ILogger, StatusCode, string, double, Exception> _requestFinishedWithExceptionGrpc = LoggerMessage.Define<StatusCode, string, double>(
            LogLevel.Error,
            Response,
            "GRPC responded with EXCEPTION {StatusCode} - {Detail} in {Elapsed:0.0000} ms"
        );

        private static readonly Action<ILogger, int, double, Exception?> _requestFinishedSlowly = LoggerMessage.Define<int, double>(
            LogLevel.Warning,
            Response,
            "HTTP responded SLOWLY {StatusCode} in {Elapsed:0.0000} ms"
        );

        private static readonly Action<ILogger, StatusCode, double, Exception?> _requestFinishedSlowlyGrpc = LoggerMessage.Define<StatusCode, double>(
            LogLevel.Warning,
            Response,
            "GRPC responded SLOWLY {StatusCode} in {Elapsed:0.0000} ms"
        );

        public static void RequestFinishedWithError(this ILogger logger, int statusCode, double elapsed) => _requestFinishedWithError(logger, statusCode, elapsed, null);

        public static void RequestFinishedSlowly(this ILogger logger, int statusCode, double elapsed) => _requestFinishedSlowly(logger, statusCode, elapsed, null);

        public static void RequestFinished(this ILogger logger, int statusCode, double elapsed) => _requestFinished(logger, statusCode, elapsed, null);

        public static void GrpcRequestFinishedWithError(this ILogger logger, Status statusCode, double elapsed) => 
            _requestFinishedGrpcWithError(logger, statusCode.StatusCode, statusCode.Detail, elapsed, statusCode.DebugException);

        public static void GrpcRequestFinishedSlowly(this ILogger logger, Status statusCode, double elapsed) =>
            _requestFinishedSlowlyGrpc(logger, statusCode.StatusCode, elapsed, null);

        public static void GrpcRequestFinished(this ILogger logger, Status statusCode, double elapsed) =>
            _requestFinishedGrpc(logger, statusCode.StatusCode, elapsed, null);

        public static void RequestFinishedWithException(this ILogger logger, Exception exception, int statusCode, double elapsed) =>
        _requestFinishedWithException(logger, statusCode, elapsed, exception);

        public static void GrpcRequestFinishedWithException(this ILogger logger, Exception exception, Status statusCode, double elapsed) =>
        _requestFinishedWithExceptionGrpc(logger, statusCode.StatusCode, statusCode.Detail, elapsed, exception);
    }
}