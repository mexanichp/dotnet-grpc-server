using System;
using System.Collections.Generic;
using Grpc.AspNetCore.Server;
using HealthyPlant.Grpc.Infrastructure.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure
{
    public sealed class MessageLog
    {
        private MessageLog() { }

        public RequestDetailsMessage? RequestDetails { get; private set; }
        public ResponseDetailsMessage? ResponseDetails { get; private set; }
        public ResponseGrpcDetailsMessage? ResponseGrpcDetails { get; private set; }
        public EnvironmentDetailsMessage? EnvironmentDetails { get; private set; }
        public IReadOnlyDictionary<string, object>? Diagnostics { get; private set; }

        public sealed class MessageLogBuilder
        {
            private readonly MessageLog _message = new MessageLog();

            public MessageLogBuilder SetRequestDetails(HttpContext context, ILogger logger)
            {
                var httpRequestFeature = context.Features.Get<IHttpRequestFeature?>();
                var httpConnectionFeature = context.Features.Get<IHttpConnectionFeature?>();
                _message.RequestDetails = RequestDetailsMessage.Build(httpRequestFeature, httpConnectionFeature, logger);

                return this;
            }

            public MessageLogBuilder SetDiagnostics(IDiagnosticContext diagnostic)
            {
                _message.Diagnostics = diagnostic.GetData();
                return this;
            }

            public MessageLogBuilder SetResponseDetails(HttpContext context, ILogger logger)
            {
                _message.ResponseDetails = ResponseDetailsMessage.Build(context.Features.Get<IHttpResponseFeature?>(), logger);
                return this;
            }

            public MessageLogBuilder SetResponseGrpcDetails(HttpContext context, ILogger logger)
            {
                _message.ResponseGrpcDetails = ResponseGrpcDetailsMessage.Build(context.Features.Get<IServerCallContextFeature?>(), logger);
                return this;
            }

            public MessageLogBuilder SetEnvironmentDetails(double elapsed, double elapsedCpuTime, ILogger logger)
            {
                _message.EnvironmentDetails = EnvironmentDetailsMessage.Build(elapsed, elapsedCpuTime, logger);
                return this;
            }

            public MessageLog Build() => _message;

            public IDisposable AddMessageToScope(ILogger logger)
            {
                return new ScopeBuilder(logger.BeginScope("{@MessageLog}", _message));
            }

            private class ScopeBuilder : IDisposable
            {
                private readonly IReadOnlyList<IDisposable> _scopes;

                public ScopeBuilder(params IDisposable[] scopes)
                {
                    _scopes = scopes;
                }

                public void Dispose()
                {
                    foreach (var disposable in _scopes)
                    {
                        disposable?.Dispose();
                    }
                }
            }
        }
    }
}