using System;
using System.Threading;

namespace Tracing.Integration.Middleware
{
    public class CorrelationIdContext
    {
        private string _correlationIdLocal;

        public string CorrelationId
        {
            get => _correlationIdLocal;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("correlation id cannot be null or whitespace");
                if (!string.IsNullOrWhiteSpace(_correlationIdLocal))
                    throw new InvalidOperationException("Correlation id is already set for this context");
                _correlationIdLocal = value;
            }
        }
    }
}