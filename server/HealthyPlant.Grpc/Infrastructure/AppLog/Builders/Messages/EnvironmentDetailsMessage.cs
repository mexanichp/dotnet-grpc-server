using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Infrastructure.Messages
{
    public class EnvironmentDetailsMessage
    {
        public int? ProcessorCount { get; private set; }

        public string? OSVersion { get; private set; }

        public string? Cpu { get; private set; }

        public long? ConsumedPhysicalMemory { get; private set; }

        public string? MachineName { get; private set; }

        public string? ApplicationVersion { get; private set; }

        private EnvironmentDetailsMessage() { }

        public static EnvironmentDetailsMessage? Build(double elapsed, double elapsedCpuTime, ILogger logger)
        {
            try
            {
                var message = new EnvironmentDetailsMessage();
                message.TrySetValue(t => t.MachineName = Environment.MachineName, logger);
                message.TrySetValue(t =>
                {
                    t.OSVersion = Environment.OSVersion.ToString();
                    t.ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
                }, logger);

                message.TrySetValue(t =>
                {
                    using var currentProcess = Process.GetCurrentProcess();
                    t.ProcessorCount = Environment.ProcessorCount;
                    t.ConsumedPhysicalMemory = currentProcess.WorkingSet64 / 0x100000; // in MB
                    t.Cpu = (elapsedCpuTime / (t.ProcessorCount.Value * elapsed) * 100).ToString("N2");
                    t.OSVersion = Environment.OSVersion.ToString();
                    t.ProcessorCount = Environment.ProcessorCount;
                }, logger);

                return message;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "An error occurred when building EnvironmentDetailsMessage.");
                return null;
            }
        }
    }

    public static class Extensions
    {
        public static void TrySetValue(this EnvironmentDetailsMessage message, Action<EnvironmentDetailsMessage> setter, ILogger logger)
        {
            try
            {
                setter.Invoke(message);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "An error occurred while building EnvironmentDetailsMessage.");
            }
        }
    }
}