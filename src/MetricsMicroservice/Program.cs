using NATS.Client;
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
                _connection.SubscribeAsync("post.metrics", "metrics-microservice-group", PostMetric),
            };

            // start the subscriptions
            subscriptions.ForEach(s => s.Start());

            // keep this console app running
            Console.WriteLine($"Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }

        private static void PostMetric(object sender, MsgHandlerEventArgs e)
        {
            // artificial processing time
            Thread.Sleep(5000);

            // extract the metric from the nats message
            var metric = Encoding.UTF8.GetString(e.Message.Data);

            Console.WriteLine($"Received the metric:\r\n{metric}");
        }
    }
}