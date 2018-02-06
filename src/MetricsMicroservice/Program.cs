using NATS.Client;
using System;
using System.Collections.Generic;
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
            var msg = MessageHelper.GetNatsMessage(e.Message);
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Console.WriteLine($"{DateTime.Now.ToShortTimeString()}: Message Called: {msg.Subject} Total Execution Time (ms): {now - msg.StartedOnUtc}");
            Console.WriteLine("\tBreakdown:");

            msg.CallTimings.ForEach(t =>
                                    {
                                        Console.WriteLine($"\t\tSubject: {t.Item1} Pattern: {t.Item2}, Execution Time (ms): {t.Item3}");
                                    });

            Console.WriteLine(new string('-', 120));
        }
    }
}