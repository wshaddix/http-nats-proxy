using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MetricsMicroservice
{
    internal class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        private static IConnection _connection;

        private static void Main(string[] args)
        {
            // configure the url to the NATS server
            var natsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222";

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            _connection = connectionFactory.CreateConnection(natsUrl);

            // setup a subscription to the "test" queues using a queue group for this microservice
            var subscriptions = new List<IAsyncSubscription>
            {
                _connection.SubscribeAsync("pipeline.metrics", "metrics-microservice-group", PostMetric),
            };

            // start the subscriptions
            subscriptions.ForEach(s => s.Start());

            // keep this console app running
            Console.WriteLine($"Metrics Microservice connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }

        private static void PostMetric(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(e.Message.Data));
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Console.WriteLine($"{DateTime.Now.ToShortTimeString()}: Message Called: {msg["subject"]} Total Execution Time (ms): {now - (long)msg["startedOnUtc"]}");
            Console.WriteLine("\tBreakdown:");

            if (msg["callTimings"] is JArray callTimings)
            {
                foreach (var token in callTimings)
                {
                    Console.WriteLine($"\t\tSubject: {token["item1"]} Pattern: {token["item2"]}, Execution Time (ms): {token["item3"]}");
                }
            }

            Console.WriteLine(new string('-', 120));
        }
    }
}