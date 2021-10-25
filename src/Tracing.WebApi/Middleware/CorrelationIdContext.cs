using System;
using System.Threading;

namespace Tracing.WebApi.Middleware
{
    public class CorrelationIdContext
    {
        private static readonly AsyncLocal<string> CorrelationIdLocal = new();

        public static string CorrelationId
        {
            get => CorrelationIdLocal.Value;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("correlation id cannot be null or whitespace");
                if (!string.IsNullOrWhiteSpace(CorrelationIdLocal.Value))
                    throw new InvalidOperationException("Correlation id is already set for this context");
                CorrelationIdLocal.Value = value;
            }
        }
    }
}