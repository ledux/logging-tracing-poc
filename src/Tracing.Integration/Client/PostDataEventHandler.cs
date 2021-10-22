using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tracing.Integration.Hosting;
using Tracing.Integration.Models;

namespace Tracing.Integration.Client
{
    public class PostDataEventHandler : IEventHandler<Data>
    {
        private readonly HttpClient _httpClient;
        private static readonly ActivitySource ActivitySource = new (nameof(PostDataEventHandler));

        public PostDataEventHandler(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
            
        public async Task Handle(Data result)
        {
            using var activity = ActivitySource.StartActivity("sending data", ActivityKind.Client);
            activity?.AddTag("correlationId", result);

            HttpContent content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");
            var responseMessage = await _httpClient.PostAsync("http://localhost:5001/integration", content);
        }
    }
}