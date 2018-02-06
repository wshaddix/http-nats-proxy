using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraceHeaderPipelineStep
{
    public class MessageHelper
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static NatsMessage GetNatsMessage(Msg msg)
        {
            return JsonConvert.DeserializeObject<NatsMessage>(Encoding.UTF8.GetString(msg.Data));
        }

        public static string GetValue(string key, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            // force the enumeration of queryParms
            var parameterList = parameters.ToList();

            // try to find the matching key
            var match = parameterList.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            // if we didn't find a match then just return an empty string
            return string.IsNullOrWhiteSpace(match.Key) ? string.Empty : match.Value;
        }

        public static byte[] PackageResponse(object data)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, SerializerSettings));
        }
    }
}