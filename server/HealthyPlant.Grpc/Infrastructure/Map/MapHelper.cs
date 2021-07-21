using System;
using Google.Protobuf.WellKnownTypes;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class MapHelper
    {
        public static string GetOrThrowIfEmpty(this string? value) =>
            string.IsNullOrEmpty(value) ? throw new ArgumentException(value) : value;

        public static DateTime? GetOrNullIfDefault(this Timestamp? timestamp) =>
            timestamp == null || timestamp == new Timestamp() ? (DateTime?)null : timestamp.ToDateTime();
    }
}