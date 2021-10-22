using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace Tracing.WebApi.Producer
{
    public class KafkaProducer
    {
        private readonly ILogger<KafkaProducer> _logger;
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(KafkaProducer));

        public KafkaProducer(ILogger<KafkaProducer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task Produce(Data data)
        {
            using var activity = ActivitySource.StartActivity("producing kafka event", ActivityKind.Producer);
            
            var config = new ProducerConfig { BootstrapServers = "localhost:29092" };
            var builder = new ProducerBuilder<string, string>(config);

            var producer = builder.Build();
            var dataAsJson = JsonSerializer.Serialize(data);
            var message = new Message<string, string> { Value = dataAsJson };

            var deliveryResult = await producer.ProduceAsync("topicname", message);
            activity.AddTag("offset", deliveryResult.Offset.Value.ToString());
            activity.AddBaggage("baggageKey", "baggage value");
            activity.SetStatus(Status.Error);
        }
    }
}