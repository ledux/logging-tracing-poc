using System.Collections.Generic;

namespace Tracing.Integration.Models
{
    public class Event<T>
    {
        public T Payload { get; set; }
        public Dictionary<string, string> Context { get; set; }
    }
}