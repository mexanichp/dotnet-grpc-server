using Grpc.Core;
using HealthyPlant.Grpc.Models;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class ContextExtensions
    {
        public static FirebaseUser GetFirebaseUser(this ServerCallContext context)
        {
            try
            {
                var identity = context.GetHttpContext().User;
                return new FirebaseUser(
                    uid: identity.GetUserId(),
                    email: identity.GetUserEmail(),
                    timezoneOffset: context.RequestHeaders.GetValue(UserDefaults.TimezoneOffset)
                );
            }
            catch
            {
                return new FirebaseUser("unauthenticated", null, null);
            }
        }
    }
}