using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace Tracing.Integration.Hosting
{
    public class KafkaListenerService<TKey, TData> : BackgroundService
    {
        private readonly IConsumer<TKey,TData> _consumer;
        private readonly IEnumerable<string> _topics;
        private readonly IEventHandler<TKey, TData> _handler;

        public KafkaListenerService(IConsumer<TKey, TData> consumer, IEnumerable<string> topics, IEventHandler<TKey, TData> handler)
        {
            _consumer = consumer;
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
                await _handler.Handle(consumeResult);
            }
        }
    }

    public interface IEventHandler<T, T1>
    {
        Task<T1> Handle(ConsumeResult<T, T1> result);
    }
}