using System.Diagnostics;
using System.Threading.Tasks;
using Tracing.Integration.Hosting;
using Tracing.Integration.Models;

namespace Tracing.Integration.Client
{
    public class PostDataEventHandler : IEventHandler<Data>
    {
        private static readonly ActivitySource ActivitySource = new (nameof(PostDataEventHandler));
            
        public async Task Handle(Data result)
        {
            using var activity = ActivitySource.StartActivity("sending data", ActivityKind.Client);
            activity?.AddTag("correlationId", result.CorrelationId);
            activity?.AddTag("email", result.Email);
        }
    }
}