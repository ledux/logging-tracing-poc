using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Tracing.Integration.Hosting;
using Tracing.Integration.Models;

namespace Tracing.Integration.Consumer
{
    public class KafkaEventHandler : IEventHandler<Data>
    {
        private static readonly ActivitySource ActivitySource = new (nameof(KafkaEventHandler));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
            
        public async Task Handle(Data result)
        {
            using var activity = ActivitySource.StartActivity("sending data", ActivityKind.Client);
            activity?.AddTag("correlationId", result.CorrelationId);
            activity?.AddTag("email", result.Email);
        }
    }
}