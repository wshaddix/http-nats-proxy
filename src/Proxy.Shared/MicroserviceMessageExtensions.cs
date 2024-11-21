using System.Text;
using Newtonsoft.Json;

namespace Proxy.Shared;

public static class MicroserviceMessageExtensions
{
    public static byte[] ToBytes(this MicroserviceMessage msg, JsonSerializerSettings serializerSettings)
    {
        var serializedMessage = JsonConvert.SerializeObject(msg, serializerSettings);
        return Encoding.UTF8.GetBytes(serializedMessage);
    }

    public static MicroserviceMessage? ToMicroserviceMessage(this byte[] data)
    {
        return JsonConvert.DeserializeObject<MicroserviceMessage>(Encoding.UTF8.GetString(data));
    }
}