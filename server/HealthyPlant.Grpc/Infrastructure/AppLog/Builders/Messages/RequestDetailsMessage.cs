using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace HealthyPlant.Grpc.Infrastructure.Messages
{
    public class RequestDetailsMessage
    {
        private RequestDetailsMessage() { }

        public string? RawTarget { get; private set; }

        public string? HttpMethod { get; private set; }

        public string? IPAddress { get; private set; }

        public IDictionary? Headers { get; private set; }

        public string? Protocol { get; private set; }

        public static RequestDetailsMessage? Build(IHttpRequestFeature? httpRequestFeature, IHttpConnectionFeature? httpConnectionFeature, ILogger logger)
        {
            try
            {
                if (httpConnectionFeature == null && httpRequestFeature == null)
                {
                    return null;
                }

                var message = new RequestDetailsMessage();
                if (httpRequestFeature != null)
                {
                    var headers = new Dictionary<string, string>();
                    foreach (var t in httpRequestFeature.Headers)
                    {
                        if (!t.Key.Equals(HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
                            headers.Add(t.Key, t.Value.ToString());
                    }

                    message.Headers = headers;
                    message.RawTarget = httpRequestFeature.RawTarget;
                    message.HttpMethod = httpRequestFeature.Method;
                    message.Protocol = httpRequestFeature.Protocol;
                }

                if (httpConnectionFeature != null)
                {
                    message.IPAddress = httpConnectionFeature.RemoteIpAddress?.ToString();
                }

                return message;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "An error occurred when building RequestDetailsMessage.");
                return null;
            }
        }
    }
}