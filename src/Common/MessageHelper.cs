using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NATS.Client;
using Newtonsoft.Json;

namespace Common
{
    public class MessageHelper
    {
        public static NatsMessage GetNatsMessage(Msg msg)
        {
            return JsonConvert.DeserializeObject<NatsMessage>(Encoding.UTF8.GetString(msg.Data));
        }

        public static T Deserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
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
    }
}