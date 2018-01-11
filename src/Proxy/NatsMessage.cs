using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Proxy
{
    internal static class NatsMessageExtensions
    {
        internal static byte[] ToBytes(this NatsMessage msg, JsonSerializerSettings serializerSettings)
        {
            var serializedMessage = JsonConvert.SerializeObject(msg, serializerSettings);
            return Encoding.UTF8.GetBytes(serializedMessage);
        }
    }

    internal sealed class NatsMessage
    {
        public string Body { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Cookies { get; set; }
        public string ErrorMessage { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        public string Host { get; set; }
        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }
        public string Response { get; set; }
        public string ResponseContentType { get; set; }
        public int ResponseStatusCode { get; set; }
        public string Subject { get; set; }
    }
}