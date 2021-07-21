using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using HealthyPlant.Grpc.Helpers;
using HealthyPlant.Grpc.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyPlant.Grpc.Infrastructure
{
    public class AppLogMiddleware
    {
        private readonly RequestDelegate _next;

        public AppLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext httpContext,
            ILogger<AppLogMiddleware> logger,
            IDiagnosticContext diagnostic,
            IOptionsSnapshot<LoggerOptions> loggerOptions
        )
        {
            var sw = ValueStopwatch.StartNew();
            using var process = Process.GetCurrentProcess();
            var startCpuTime = process.TotalProcessorTime;
            try
            {
                await _next(httpContext);

                var elapsed = sw.GetElapsedTime().TotalMilliseconds;
                var elapsedCpuTime = (process.TotalProcessorTime - startCpuTime).TotalMilliseconds;

                var ctx = httpContext.Features.Get<IServerCallContextFeature>();
                if (ctx?.ServerCallContext == null)
                {
                    LogHttpRequestFinished(httpContext, logger, diagnostic, loggerOptions.Value.ExpectElapsedLessThanMs,
                        elapsed, elapsedCpuTime);
                }
                else
                {
                    LogGrpcRequestFinished(httpContext, logger, diagnostic, ctx.ServerCallContext,
                        loggerOptions.Value.ExpectElapsedLessThanMs, elapsed, elapsedCpuTime);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred.");

                var elapsed = sw.GetElapsedTime().TotalMilliseconds;
                var elapsedCpuTime = (process.TotalProcessorTime - startCpuTime).TotalMilliseconds;

                var ctx = httpContext.Features.Get<IServerCallContextFeature>();
                if (ctx?.ServerCallContext == null)
                {
                    LogHttpRequestFinishedWithException(ex, httpContext, logger, diagnostic, elapsed, elapsedCpuTime);
                }
                else
                {
                    LogGrpcRequestFinishedWithException(ex, httpContext, logger, diagnostic, ctx.ServerCallContext, elapsed, elapsedCpuTime);
                }


                throw;
            }
        }

        private void LogGrpcRequestFinished(HttpContext httpContext,
            ILogger<AppLogMiddleware> logger,
            IDiagnosticContext diagnostic,
            ServerCallContext serverCallContext,
            double expectElapsedLessThan,
            double elapsed, 
            double elapsedCpuTime)
        {
            try
            {
                var status = serverCallContext.Status;
                if (status.StatusCode != StatusCode.OK)
                {
                    using var logScope = new MessageLog.MessageLogBuilder()
                        .SetRequestDetails(httpContext, logger)
                        .SetDiagnostics(diagnostic)
                        .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                        .SetResponseDetails(httpContext, logger)
                        .SetResponseGrpcDetails(httpContext, logger)
                        .AddMessageToScope(logger);

                    logger.GrpcRequestFinishedWithError(status, elapsed);
                    return;
                }

                if (elapsed > expectElapsedLessThan && logger.IsEnabled(LogLevel.Warning))
                {
                    using var logScope = new MessageLog.MessageLogBuilder()
                        .SetRequestDetails(httpContext, logger)
                        .SetDiagnostics(diagnostic)
                        .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                        .SetResponseDetails(httpContext, logger)
                        .SetResponseGrpcDetails(httpContext, logger)
                        .AddMessageToScope(logger);

                    logger.GrpcRequestFinishedSlowly(status, elapsed);
                    return;
                }

                if (logger.IsEnabled(LogLevel.Information)) // TODO: test dynamically change of log level.
                {
                    logger.GrpcRequestFinished(status, elapsed);
                }
            }
            catch (Exception logException)
            {
                logger.LogWarning(logException, "Couldn't process RequestFinished log.");
            }
        }

        private void LogHttpRequestFinished(
            HttpContext httpContext, 
            ILogger<AppLogMiddleware> logger, 
            IDiagnosticContext diagnostic, 
            double expectElapsedLessThan, 
            double elapsed,
            double elapsedCpuTime
        )
        {
            try
            {
                var statusCode = httpContext.Response.StatusCode;
                if (statusCode == 408 || statusCode >= 500)
                {
                    using var logScope = new MessageLog.MessageLogBuilder()
                        .SetRequestDetails(httpContext, logger)
                        .SetDiagnostics(diagnostic)
                        .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                        .SetResponseDetails(httpContext, logger)
                        .AddMessageToScope(logger);

                    logger.RequestFinishedWithError(statusCode, elapsed);
                    return;
                }

                if (elapsed > expectElapsedLessThan && logger.IsEnabled(LogLevel.Warning))
                {
                    using var logScope = new MessageLog.MessageLogBuilder()
                        .SetRequestDetails(httpContext, logger)
                        .SetDiagnostics(diagnostic)
                        .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                        .SetResponseDetails(httpContext, logger)
                        .AddMessageToScope(logger);

                    logger.RequestFinishedSlowly(statusCode, elapsed);
                    return;
                }

                if (logger.IsEnabled(LogLevel.Information)) // TODO: test dynamically change of log level.
                {
                    using var logScope = new MessageLog.MessageLogBuilder()
                        .SetRequestDetails(httpContext, logger)
                        .SetDiagnostics(diagnostic)
                        .SetEnvironmentDetails(elapsed, elapsed, logger)
                        .SetResponseDetails(httpContext, logger)
                        .AddMessageToScope(logger);

                    logger.RequestFinished(statusCode, elapsed);
                }
            }
            catch (Exception logException)
            {
                logger.LogWarning(logException, "Couldn't process RequestFinished log.");
            }
        }

        private void LogGrpcRequestFinishedWithException(
            Exception ex, 
            HttpContext httpContext, 
            ILogger<AppLogMiddleware> logger, 
            IDiagnosticContext diagnostic, 
            ServerCallContext serverCallContext, 
            double elapsed,
            double elapsedCpuTime
        )
        {
            try
            {
                var status = serverCallContext.Status;
                using var logScope = new MessageLog.MessageLogBuilder()
                    .SetRequestDetails(httpContext, logger)
                    .SetDiagnostics(diagnostic)
                    .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                    .SetResponseDetails(httpContext, logger)
                    .SetResponseGrpcDetails(httpContext, logger)
                    .AddMessageToScope(logger);

                logger.GrpcRequestFinishedWithException(ex, status, elapsed);
            }
            catch (Exception logException)
            {
                logger.LogWarning(logException, "Couldn't process RequestFinished log.");
            }
        }

        private void LogHttpRequestFinishedWithException(
            Exception ex, 
            HttpContext httpContext, 
            ILogger<AppLogMiddleware> logger, 
            IDiagnosticContext diagnostic, 
            double elapsed,
            double elapsedCpuTime
        )
        {
            try
            {
                var statusCode = httpContext.Response.StatusCode;
                using var logScope = new MessageLog.MessageLogBuilder()
                    .SetRequestDetails(httpContext, logger)
                    .SetDiagnostics(diagnostic)
                    .SetEnvironmentDetails(elapsed, elapsedCpuTime, logger)
                    .SetResponseDetails(httpContext, logger)
                    .AddMessageToScope(logger);

                logger.RequestFinishedWithException(ex, statusCode, elapsed);
            }
            catch (Exception logException)
            {
                logger.LogWarning(logException, "Couldn't process RequestFinished log.");
            }
        }
    }
}