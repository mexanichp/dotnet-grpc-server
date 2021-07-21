using System;
using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure.Messages
{
    public class ResponseDetailsMessage
    {
        private ResponseDetailsMessage() { }

        public int? StatusCode { get; private set; }

        public IDictionary? Headers { get; private set; }

        public static ResponseDetailsMessage? Build(IHttpResponseFeature? httpResponseFeature, ILogger logger)
        {
            try
            {
                if (httpResponseFeature == null || httpResponseFeature.HasStarted)
                {
                    return null;
                }

                return new ResponseDetailsMessage
                {
                    Headers = httpResponseFeature.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()),
                    StatusCode = httpResponseFeature.StatusCode
                };
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "An error occurred when building ResponseDetailsMessage.");
                return null;
            }
        }
}}