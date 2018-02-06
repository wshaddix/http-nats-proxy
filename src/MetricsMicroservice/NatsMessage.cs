using System;
using System.Collections.Generic;

namespace MetricsMicroservice
{
    public sealed class NatsMessage
    {
        public string Body { get; set; }
        public List<(string, string, long)> CallTimings { get; set; }
        public long CompletedOnUtc { get; set; }
        public List<KeyValuePair<string, string>> Cookies { get; set; }
        public string ErrorMessage { get; set; }
        public long ExecutionTimeMs => CompletedOnUtc - StartedOnUtc;
        public Dictionary<string, string> ExtendedProperties { get; set; }
        public string Host { get; set; }
        public List<KeyValuePair<string, string>> QueryParams { get; set; }
        public List<KeyValuePair<string, string>> RequestHeaders { get; set; }
        public string Response { get; set; }
        public string ResponseContentType { get; set; }
        public List<KeyValuePair<string, string>> ResponseHeaders { get; set; }
        public int ResponseStatusCode { get; set; }
        public long StartedOnUtc { get; set; }
        public string Subject { get; set; }

        public NatsMessage(string host, string contentType)
        {
            // capture the time in epoch utc that this message was started
            StartedOnUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // default the response status code to an invalid value for comparison later on when the response is being processed by the RequestHandler
            ResponseStatusCode = -1;

            // capture the host machine that we're executing on
            Host = host;

            // capture the content type for the http response that we're configured for
            ResponseContentType = contentType;

            // initialize the default properties
            Cookies = new List<KeyValuePair<string, string>>();
            ExtendedProperties = new Dictionary<string, string>();
            QueryParams = new List<KeyValuePair<string, string>>();
            RequestHeaders = new List<KeyValuePair<string, string>>();
            ResponseHeaders = new List<KeyValuePair<string, string>>();
        }

        public void MarkComplete()
        {
            CompletedOnUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}