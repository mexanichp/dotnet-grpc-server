using System;

namespace HealthyPlant.Grpc.Models
{
    public class FirebaseUser
    {
        public FirebaseUser(string uid, string? email, string? timezoneOffset)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            Email = email;
            TimezoneOffset = timezoneOffset;
        }

        public string Uid { get; }

        public string? Email { get; }

        public string? TimezoneOffset { get; }
    }
}