using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Tracing.WebApi.Models;

namespace Tracing.WebApi.Producer
{
    public class KafkaProducer
    {
        private readonly ILogger<KafkaProducer> _logger;
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(KafkaProducer));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

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

            var eventData = new Event<Data> { Payload = data };
            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), eventData, InjectContext);
            
            var dataAsJson = JsonSerializer.Serialize(eventData);
            var message = new Message<string, string> { Value = dataAsJson };

            var deliveryResult = await producer.ProduceAsync("topicname", message);
            
            activity?.AddTag("offset", deliveryResult.Offset.Value.ToString());
            activity?.AddBaggage("baggageKey", "baggage value");
            activity.SetStatus(Status.Error);
        }

        private void InjectContext(Event<Data> eventData, string key, string value)
        {
            eventData.Context ??= new Dictionary<string, string>();
            eventData.Context[key] = value;
        }
    }
}