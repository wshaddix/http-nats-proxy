using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AuthenticationPipelineStep
{
    internal class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static IConnection _connection;

        private static void EnsureAuthHeaderPresent(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(e.Message.Data));

            // if the msg doesn't include an authentication header we need to return a redirect to the auth url
            var authHeader = msg.SelectToken("requestHeaders.authorization");

            if (null == authHeader)
            {
                Console.WriteLine($"No Authorization header found. Returning a 301 redirect to http://google.com");
                msg["responseStatusCode"] = 301;
                msg["responseHeaders"]["Location"] = "https://google.com";
                msg["shouldTerminateRequest"] = true;
            }
            else
            {
                Console.WriteLine($"Authorization header was already on the message so nothing to do.");
            }

            // send the NATS message back to the caller
            _connection.Publish(e.Message.Reply, PackageResponse(msg));
        }

        private static void Main(string[] args)
        {
            // configure the url to the NATS server
            var natsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222";

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            _connection = connectionFactory.CreateConnection(natsUrl);

            // setup a subscription to the "authentication" queue using a queue group for this microservice
            var subscriptions = new List<IAsyncSubscription>
            {
                _connection.SubscribeAsync("authentication", "authentication-microservice-group", EnsureAuthHeaderPresent),
            };

            // start the subscriptions
            subscriptions.ForEach(s => s.Start());

            // keep this console app running
            Console.WriteLine($"Authentication Pipeline Step Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }

        private static byte[] PackageResponse(object data)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, SerializerSettings));
        }
    }
}