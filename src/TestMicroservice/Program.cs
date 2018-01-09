using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NATS.Client;

namespace TestMicroservice
{
    class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
        private static IConnection _connection;

        static void Main(string[] args)
        {
            // configure the url to the NATS server
            var natsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222";

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            _connection = connectionFactory.CreateConnection(natsUrl);

            // setup a subscription to the "test" queues using a queue group for this microservice
            var subscriptions = new List<IAsyncSubscription>
            {
                _connection.SubscribeAsync("get.test.v1.customer", "test-microservice-group", GetCustomer),
                _connection.SubscribeAsync("post.test.v1.customer", "test-microservice-group", PostCustomer),
                _connection.SubscribeAsync("put.test.v1.customer", "test-microservice-group", PutCustomer)
            };
            
            // start the subscriptions
            subscriptions.ForEach(s => s.Start());
            
            // keep this console app running
            Console.WriteLine($"Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }

        private static void GetCustomer(object sender, MsgHandlerEventArgs e)
        {
            var reply = Encoding.UTF8.GetBytes("I got the GET message");

            _connection.Publish(e.Message.Reply, reply);
        }

        private static void PostCustomer(object sender, MsgHandlerEventArgs e)
        {
            var reply = Encoding.UTF8.GetBytes("I got the POST message");

            _connection.Publish(e.Message.Reply, reply);
        }

        private static void PutCustomer(object sender, MsgHandlerEventArgs e)
        {
            var reply = Encoding.UTF8.GetBytes("I got the PUT message");

            _connection.Publish(e.Message.Reply, reply);
        }
    }
}
