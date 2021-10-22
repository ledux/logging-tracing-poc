using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Tracing.Integration.Models;

namespace Tracing.Integration.Consumer
{
    public class KafkaConsumer
    {
        public void Consume(CancellationToken token, Action<Data> handle)
        {
            ConsumerConfig config = new ConsumerConfig()
            {
                BootstrapServers = "localhost:92029",
                GroupId = "consumergroup",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true 
            };
            using (var consumer = new ConsumerBuilder<string, string>(config).Build())
            {
                consumer.Subscribe(new []{"topicname"});
                while (!token.IsCancellationRequested)
                {
                    var consumedResult = consumer.Consume(token);
                }
                
                consumer.Close();
            }
        }
    }
}