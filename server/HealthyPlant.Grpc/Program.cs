using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddSeq(context.Configuration.GetSection("SeqConfig"));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    var url = $"http://0.0.0.0:{port}";

                    webBuilder.ConfigureKestrel(op =>
                    {
                        op.AddServerHeader = false;
                    });
                    webBuilder.UseStartup<Startup>().UseUrls(url);
                });
    }
}
