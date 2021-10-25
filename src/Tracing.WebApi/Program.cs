using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Tracing.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((context, provider, config) =>
                {
                    config
                        .MinimumLevel.Information()
                        .Enrich.WithCorrelationIdHeader("correlationId")
                        .Enrich.FromLogContext()
                        // .WriteTo.Console()
                        
                        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
                        {
                            AutoRegisterTemplate = true,
                            IndexFormat = "applogs-{0:yyyy-MM-dd}-0",
                            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
                        });
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.IncludeScopes = true;
                        options.ParseStateValues = true;
                        options.IncludeFormattedMessage = true;
                        options.AddConsoleExporter();
                    });
                    builder.Configure(options =>
                    {
                        options.ActivityTrackingOptions =
                            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId |
                            ActivityTrackingOptions.TraceFlags | ActivityTrackingOptions.TraceState;
                    });
                });
    }
}
