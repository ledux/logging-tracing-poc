using System;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tracing.Integration.Client;
using Tracing.Integration.Hosting;
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
                        .AddHostedService(provider =>
                        {
                            var postDataEventHandler = provider.GetService<PostDataEventHandler>();
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
                        .AddHttpClient<PostDataEventHandler>()
                        ;
                    
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
                .Build().Run();
        }
    }
}