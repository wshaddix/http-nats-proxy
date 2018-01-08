using System;
using System.Text;
using System.Threading;
using NATS.Client;

namespace TestMicroservice
{
    class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        private static IConnection _connection;

        static void Main(string[] args)
        {
            // configure the url to the NATS server
            var natsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222";

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            _connection = connectionFactory.CreateConnection(natsUrl);

            // setup a subscription to the "test" queue using a queue group for this microservice
            var subscription = _connection.SubscribeAsync("get.test.customer", "test-microservice-group");
            subscription.MessageHandler += GetCustomer;
            subscription.Start();


            // keep this console app running
            Console.WriteLine($"Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            _manualResetEvent.WaitOne();
        }

        private static void GetCustomer(object sender, MsgHandlerEventArgs e)
        {
            var reply = Encoding.UTF8.GetBytes("I got the message");

            _connection.Publish(e.Message.Reply, reply);
        }
    }
}
