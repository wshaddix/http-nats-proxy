using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Proxy.Shared
{
    public sealed class MicroserviceMessage
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

        public MicroserviceMessage(string host, string contentType)
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

        public void SetResponse(object response)
        {
            ResponseBody = JsonConvert.SerializeObject(response);
        }

        public bool TryGetParam<T>(string key, out T value)
        {
            // try to find a parameter with matching name across the cookies, query parameters, request headers and extended properties
            if (QueryParams.TryGetValue(key, out var queryParamValue))
            {
                value = (T)queryParamValue;
                return true;
            }

            if (RequestHeaders.TryGetValue(key, out var headerValue))
            {
                value = (T)headerValue;
                return true;
            }

            if (Cookies.TryGetValue(key, out var cookieValue))
            {
                value = (T)cookieValue;
                return true;
            }

            if (ExtendedProperties.TryGetValue(key, out var extendedValue))
            {
                value = (T)extendedValue;
                return true;
            }

            // the parameter doesn't exist so return null/false
            value = default(T);
            return false;
        }
    }
}