using System;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Tracing.Integration.Client;
using Tracing.Integration.Hosting;
using Tracing.Integration.Middleware;
using Tracing.Integration.Models;

namespace Tracing.Integration
{
    class Program
    {
        static void Main(string[] args)
        {
            new HostBuilder()
                .ConfigureServices((context, collection) =>
                {
                    collection
                        .AddTransient<IEventHandler<Data>, PostDataEventHandler>()
                        .AddScoped<CorrelationIdContext>()
                        .AddHostedService(provider =>
                        {
                            ConsumerConfig config = new ConsumerConfig()
                            {
                                BootstrapServers = "localhost:29092",
                                GroupId = "consumergroup",
                                AutoOffsetReset = AutoOffsetReset.Earliest,
                                EnableAutoCommit = true
                            };
                            var builder = new ConsumerBuilder<string, string>(config)
                                .SetErrorHandler((consumer, error) =>
                                {
                                    Console.WriteLine(error.Reason);
                                })
                                .SetLogHandler((consumer, message) =>
                                {
                                    Console.WriteLine(message);
                                })
                                .Build();
                            return new KafkaListenerService<Data>(builder, new[] { "topicname" }, provider);
                        })
                        .AddHttpClient<PostDataEventHandler>();
                    
                        Sdk.CreateTracerProviderBuilder()
                            .AddSource(nameof(KafkaListenerService<Data>))
                            .AddSource(nameof(PostDataEventHandler))
                            .AddHttpClientInstrumentation(options => options.RecordException = true)
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Tracing.Integration"))
                            .AddJaegerExporter(options =>
                            {
                                options.AgentHost = "localhost";
                                options.AgentPort = 6831;
                            })
                            .Build();
                })
                .UseSerilog((context, provider, config) =>
                {
                    config
                        .MinimumLevel.Debug()
                        .Enrich.FromLogContext()
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
                })
                .Build().Run();
        }
    }
}