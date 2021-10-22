using System.Collections.Generic;

namespace Tracing.WebApi.Models
{
    public class Event<T>
    {
        public T Payload { get; set; }
        public Dictionary<string, string> Context { get; set; }
    }
}