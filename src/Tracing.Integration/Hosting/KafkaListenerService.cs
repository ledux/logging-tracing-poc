using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Tracing.Integration.Models;

namespace Tracing.Integration.Hosting
{
    public class KafkaListenerService<TData> : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IEnumerable<string> _topics;
        private readonly IServiceProvider _serviceProvider;
        // private readonly IEventHandler<TData> _handler;
        private static readonly ActivitySource ActivitySource = new(nameof(KafkaListenerService<TData>));
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public KafkaListenerService(IConsumer<string, string> consumer, IEnumerable<string> topics,
            IServiceProvider serviceProvider)
        {
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _topics = topics ?? throw new ArgumentNullException(nameof(topics));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            // _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_topics);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    ConsumeResult<string, string> consumeResult = null;
                    try
                    {
                        consumeResult = _consumer.Consume();
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Terminating kafka listener...");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }

                    if (consumeResult != null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetService<IEventHandler<TData>>();
                            var eventData = JsonSerializer.Deserialize<Event<TData>>(consumeResult.Message.Value);
                            var parentContext = Propagator.Extract(default, eventData.Context, ExtractContext);
                            Baggage.Current = parentContext.Baggage;

                            using var startActivity = ActivitySource.StartActivity("handling kafka event",
                                ActivityKind.Consumer,
                                parentContext.ActivityContext);
                            startActivity?.AddEvent(new ActivityEvent("event received"));

                            try
                            {
                                await handler.Handle(eventData.Payload);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                }
            }).ConfigureAwait(true);
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