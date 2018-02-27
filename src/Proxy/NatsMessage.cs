using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proxy
{
    public class CallTiming
    {
        public long EllapsedMs { get; set; }

        public string Subject { get; set; }

        public CallTiming(string subject, long ellapsedMs)
        {
            Subject = subject;
            EllapsedMs = ellapsedMs;
        }
    }

    public sealed class NatsMessage
    {
        public List<CallTiming> CallTimings { get; set; }
        public long CompletedOnUtc { get; set; }
        public Dictionary<string, object> Cookies { get; set; }
        public string ErrorMessage { get; set; }
        public long ExecutionTimeMs => CompletedOnUtc - StartedOnUtc;
        public Dictionary<string, object> ExtendedProperties { get; set; }
        public string Host { get; set; }
        public Dictionary<string, object> QueryParams { get; set; }
        public string RequestBody { get; set; }
        public Dictionary<string, object> RequestHeaders { get; set; }
        public string ResponseBody { get; set; }
        public string ResponseContentType { get; set; }
        public Dictionary<string, object> ResponseHeaders { get; set; }
        public int ResponseStatusCode { get; set; }
        public bool ShouldTerminateRequest { get; set; }
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
            Cookies = new Dictionary<string, object>();
            ExtendedProperties = new Dictionary<string, object>();
            QueryParams = new Dictionary<string, object>();
            RequestHeaders = new Dictionary<string, object>();
            ResponseHeaders = new Dictionary<string, object>();
            CallTimings = new List<CallTiming>();
        }

        public void MarkComplete()
        {
            CompletedOnUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    internal static class NatsMessageExtensions
    {
        internal static byte[] ToBytes(this NatsMessage msg, JsonSerializerSettings serializerSettings)
        {
            var serializedMessage = JsonConvert.SerializeObject(msg, serializerSettings);
            return Encoding.UTF8.GetBytes(serializedMessage);
        }
    }
}