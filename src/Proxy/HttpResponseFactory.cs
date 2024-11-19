using Newtonsoft.Json;
using Proxy.Shared;

namespace Proxy
{
    internal class HttpResponseFactory
    {
        internal static async void PrepareResponseAsync(HttpResponse httpResponse,
            MicroserviceMessage message, JsonSerializerSettings jsonSerializerSettings)
        {
            // set the status code
            httpResponse.StatusCode = message.ResponseStatusCode;

            // set the response type
            httpResponse.ContentType = message.ResponseContentType;

            // set any response headers
            foreach (var header in message.ResponseHeaders)
            {
                httpResponse.GetTypedHeaders().Append(header.Key, header.Value);
            }

            // if the message includes an error add it
            if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
            {
                var response = new
                {
                    message.ErrorMessage
                };

                await httpResponse.WriteAsync(JsonConvert.SerializeObject(response, jsonSerializerSettings)).ConfigureAwait(false);
            }

            // if the message includes a response body add it
            if (!string.IsNullOrWhiteSpace(message.ResponseBody))
            {
                await httpResponse.WriteAsync(message.ResponseBody).ConfigureAwait(false);
            }
        }
    }
}