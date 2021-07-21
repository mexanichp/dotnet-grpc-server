namespace HealthyPlant.Grpc.Infrastructure.Options
{
    public class LoggerOptions
    {
        public double ExpectElapsedLessThanMs { get; set; }
        public double ExpectedDbDurationLessThanMs { get; set; }
    }
}