using Newtonsoft.Json;

namespace Proxy.Shared
{
    public sealed class MicroserviceMessage(string host, string contentType)
    {
        public List<CallTiming> CallTimings { get; set; } = new();
        public long CompletedOnUtc { get; set; }
        public Dictionary<string, object> Cookies { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs => CompletedOnUtc - StartedOnUtc;
        public Dictionary<string, object> ExtendedProperties { get; set; } = new();
        public string Host { get; set; } = host;
        public Dictionary<string, object> QueryParams { get; set; } = new();
        public string? RequestBody { get; set; }
        public Dictionary<string, object> RequestHeaders { get; set; } = new();
        public string? ResponseBody { get; set; }
        public string ResponseContentType { get; set; } = contentType;
        public Dictionary<string, object> ResponseHeaders { get; set; } = new();
        public int ResponseStatusCode { get; set; } = -1;
        public bool ShouldTerminateRequest { get; set; }
        public long StartedOnUtc { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string? Subject { get; set; }

        // capture the time in epoch utc that this message was started
        // default the response status code to an invalid value for comparison later on when the response is being processed by the RequestHandler
        // capture the host machine that we're executing on
        // capture the content type for the http response that we're configured for
        // initialize the default properties

        public void MarkComplete()
        {
            CompletedOnUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void SetResponse(object response)
        {
            ResponseBody = JsonConvert.SerializeObject(response);
        }

        public bool TryGetParam<T>(string key, out T? value)
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