using System.Collections.Generic;

namespace Proxy
{
    internal sealed class NatsMessage
    {
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Cookies { get; set; }
        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; set; }
        public string Body { get; set; }
    }
}