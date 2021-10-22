using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Tracing.Integration.Hosting;
using Tracing.Integration.Models;

namespace Tracing.Integration.Consumer
{
    public class KafkaEventHandler : IEventHandler<string, string>
    {
        private static readonly ActivitySource ActivitySource = new (nameof(KafkaEventHandler));
            
        public async Task<string> Handle(ConsumeResult<string, string> result)
        {
            using var startActivity = ActivitySource.StartActivity("handling kafka event", ActivityKind.Client);
            var data = JsonSerializer.Deserialize<Data>(result.Message.Value);
            startActivity?.AddTag("email", data?.Email);

            return "";
        }
    }
}