using System;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure.Messages
{
    public class ResponseGrpcDetailsMessage
    {
        private ResponseGrpcDetailsMessage() { }

        public string? Peer { get; private set; }

        public string? Method { get; private set; }

        public StatusCode? StatusCode { get; private set; }
        
        public string? Detail { get; private set; }

        public static ResponseGrpcDetailsMessage? Build(IServerCallContextFeature? callContextFeature, ILogger logger)
        {
            try
            {
                if (callContextFeature == null)
                {
                    return null;
                }

                return new ResponseGrpcDetailsMessage
                {
                    Peer = callContextFeature.ServerCallContext.Peer,
                    StatusCode = callContextFeature.ServerCallContext.Status.StatusCode,
                    Detail = callContextFeature.ServerCallContext.Status.Detail,
                    Method = callContextFeature.ServerCallContext.Method
                };
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "An error occurred when building ResponseDetailsMessage.");
                return null;
            }
        }
    }
}