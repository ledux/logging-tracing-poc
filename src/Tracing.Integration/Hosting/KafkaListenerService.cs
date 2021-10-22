using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Tracing.Integration.Consumer;
using Tracing.Integration.Models;

namespace Tracing.Integration.Hosting
{
    public class KafkaListenerService<TData> : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IEnumerable<string> _topics;
        private readonly IEventHandler<TData> _handler;
        private static readonly ActivitySource ActivitySource = new(nameof(KafkaListenerService<TData>));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public KafkaListenerService(IConsumer<string, string> consumer, IEnumerable<string> topics,
            IEventHandler<TData> handler)
        {
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _topics = topics ?? throw new ArgumentNullException(nameof(topics));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_topics);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                var eventData = JsonSerializer.Deserialize<Event<TData>>(consumeResult.Message.Value);
                var parentContext = Propagator.Extract(default, eventData.Context, ExtractContext);
                Baggage.Current = parentContext.Baggage;

                using var startActivity = ActivitySource.StartActivity("handling kafka event", ActivityKind.Consumer,
                    parentContext.ActivityContext);
                startActivity?.AddEvent(new ActivityEvent("event received"));

                await _handler.Handle(eventData.Payload);
            }
        }

        private IEnumerable<string> ExtractContext(Dictionary<string, string> carrier, string key)
        {
            if (carrier.TryGetValue(key, out var value))
            {
                return new[] { value };
            }

            return Enumerable.Empty<string>();
        }
    }

    public interface IEventHandler<in TData>
    {
        Task Handle(TData data);
    }
}