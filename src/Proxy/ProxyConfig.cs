using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Proxy
{
    public class ProxyConfig
    {
        public JsonSerializerSettings JsonSerializerSettings;
        public bool AddTraceHeader => !string.IsNullOrEmpty(TraceHeaderName);
        public string ContentType { get; set; }
        public int DeleteStatusCode { get; set; }
        public int GetStatusCode { get; set; }
        public int HeadStatusCode { get; set; }
        public string Host { get; set; }
        public string LogsSubject { get; set; }
        public string MetricsSubject { get; set; }
        public IConnection NatsConnection { get; set; }
        public string NatsUrl { get; set; }
        public int PatchStatusCode { get; set; }
        public string Port { get; set; }
        public int PostStatusCode { get; set; }
        public bool PublishLogs => !string.IsNullOrEmpty(LogsSubject);
        public bool PublishMetrics => !string.IsNullOrEmpty(MetricsSubject);
        public int PutStatusCode { get; set; }
        public int Timeout { get; set; }
        public string TraceHeaderName { get; set; }

        public ProxyConfig()
        {
            Host = Environment.MachineName;

            // configure the json serializer settings to use
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }
    }
}