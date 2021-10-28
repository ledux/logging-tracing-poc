using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracing.Integration.Hosting;
using Tracing.Integration.Middleware;
using Tracing.Integration.Models;

namespace Tracing.Integration.Client
{
    public class PostDataEventHandler : IEventHandler<Data>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PostDataEventHandler> _logger;
        private readonly CorrelationIdContext _correlationIdContext;
        private static readonly ActivitySource ActivitySource = new (nameof(PostDataEventHandler));

        public PostDataEventHandler(HttpClient httpClient, ILogger<PostDataEventHandler> logger, CorrelationIdContext correlationIdContext)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _correlationIdContext = correlationIdContext ?? throw new ArgumentNullException(nameof(correlationIdContext));
        }
            
        public async Task Handle(Data result)
        {
            using var activity = ActivitySource.StartActivity("sending data", ActivityKind.Client);
            activity?.AddTag("correlationId", _correlationIdContext.CorrelationId);
            _httpClient.DefaultRequestHeaders.Add("correlation-id", _correlationIdContext.CorrelationId);
            
            _logger.LogInformation("Handling data with email {Email} for correlationId {CorrelationId}", result.Email, _correlationIdContext.CorrelationId);

            HttpContent content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json");
            var responseMessage = await _httpClient.PostAsync("http://localhost:5001/integration", content);
        }
    }
}