using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Tracing.WebApi.Middleware;
using Tracing.WebApi.Models;
using Tracing.WebApi.Producer;

namespace Tracing.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly KafkaProducer _kafkaProducer;
        private readonly TracerProvider _tracerProvider;
        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(DataController));
        private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

        public DataController(ILogger<DataController> logger, KafkaProducer kafkaProducer, TracerProvider tracerProvider)
        {
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _tracerProvider = tracerProvider;
        }

        [HttpPost]
        public async Task<Data> Post(Data data)
        {
            using var fistActivity = ActivitySource.StartActivity("First activity", ActivityKind.Server);
            var correlationId = Guid.NewGuid().ToString();
            data.CorrelationId = CorrelationIdContext.CorrelationId;
            fistActivity?.AddTag("correlationId", correlationId);
            fistActivity?.AddEvent(new ActivityEvent("something happened", DateTimeOffset.UtcNow, new ActivityTagsCollection { new("eventKey", "event value") }));

            _logger.LogInformation("Got request from email: {Email}", data.Email ?? "<NO MAIL>");
            using var secondAcivty = ActivitySource.StartActivity("second activity", ActivityKind.Server);
            await _kafkaProducer.Produce(data);

            return data;
        }
    }
}
