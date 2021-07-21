using System;

namespace Infrastructure.DateTimeExtensions
{
    public static class DateTimeExtensions
    {
        public static DateTimeOffset ToOffsetUtcDate(this DateTimeOffset date, TimeSpan offset) => new(DateTime.SpecifyKind(date.ToOffset(offset).Date, DateTimeKind.Utc));
        public static DateTimeOffset ToOffsetUtcTime(this DateTimeOffset date, TimeSpan offset) => new(DateTime.SpecifyKind(date.ToOffset(offset).DateTime, DateTimeKind.Utc));
    }
}