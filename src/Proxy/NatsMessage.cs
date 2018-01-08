using System.Collections.Generic;

namespace Proxy
{
    internal sealed class NatsMessage
    {
        public IEnumerable<KeyValuePair<string, string>> Headers { get; internal set; }
        public IEnumerable<KeyValuePair<string, string>> Cookies { get; internal set; }
        public IEnumerable<KeyValuePair<string, string>> QueryParams { get; internal set; }
        public string Body { get; internal set; }
    }
}