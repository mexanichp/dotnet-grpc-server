using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace HealthyPlant.Grpc.Infrastructure
{
    public class GrpcLogInterceptor : Interceptor
    {
        private readonly IDiagnosticContext _diagnosticContext;

        public GrpcLogInterceptor(IDiagnosticContext diagnosticContext)
        {
            _diagnosticContext = diagnosticContext;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                _diagnosticContext.Set("FirebaseUser", context.GetFirebaseUser());
                return await base.UnaryServerHandler(request, context, continuation);
            }
            catch
            {
                _diagnosticContext.Set("RequestMessage", request);
                throw;
            }
        }
    }
}