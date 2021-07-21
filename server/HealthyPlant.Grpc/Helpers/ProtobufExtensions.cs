using System.Diagnostics.Contracts;
using Google.Protobuf.WellKnownTypes;

namespace HealthyPlant.Grpc.Helpers
{
    public static class ProtobufExtensions
    {
        private const long SecondsInDay = 24 * 60 * 60;

        [Pure]
        public static Timestamp AddDays(this Timestamp timestamp, long days)
        {
            var clone = new Timestamp(timestamp);
            clone.Seconds += days * SecondsInDay;
            return clone;
        }

        [Pure]
        public static Timestamp AddSeconds(this Timestamp timestamp, long seconds)
        {
            var clone = new Timestamp(timestamp);
            clone.Seconds += seconds;
            return clone;
        } 
    }
}