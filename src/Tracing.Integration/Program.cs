using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tracing.Integration.Consumer;
using Tracing.Integration.Hosting;

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
                        .AddSingleton<IHostedService>(provider =>
                        {
                            ConsumerConfig config = new ConsumerConfig()
                            {
                                BootstrapServers = "localhost:29092",
                                GroupId = "consumergroup",
                                AutoOffsetReset = AutoOffsetReset.Earliest,
                                EnableAutoCommit = true
                            };
                            var builder = new ConsumerBuilder<string, string>(config).Build();
                            var kafkaEventHandler = new KafkaEventHandler();
                            return new KafkaListenerService<string, string>(builder, new[] { "topicname" }, kafkaEventHandler);
                        });
                    
                        Sdk.CreateTracerProviderBuilder()
                            .AddAspNetCoreInstrumentation()
                            .AddSource(nameof(KafkaListenerService<string, string>))
                            .AddSource(nameof(KafkaEventHandler))
                            .AddHttpClientInstrumentation(options => options.RecordException = true)
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Tracing.Integration"))
                            .AddJaegerExporter(options =>
                            {
                                options.AgentHost = "localhost";
                                options.AgentPort = 6831;
                            })
                            .Build();
                })
                .RunConsoleAsync();
        }
    }
}