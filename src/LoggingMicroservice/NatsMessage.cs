using System.Collections.Generic;

namespace LoggingMicroservice
{
    public sealed class NatsMessage
    {
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Cookies { get; set; }
        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }
        public string Body { get; set; }
        public string Host { get; set; }
        public string Subject { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public string Response { get; set; }
        public int ResponseStatusCode { get; set; }
        public string ResponseContentType { get; set; }
        public string ErrorMessage { get; set; }
    }
}