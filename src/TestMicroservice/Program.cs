using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TestMicroservice
{
    internal class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        private static IConnection _connection;

        private static void DeleteCustomer(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // let's simulate an error
            msg.ErrorMessage = "You cannot delete this customer because there are unpaid invoices";

            // send the NATS message (with the error message now set) back to the caller
            _connection.Publish(e.Message.Reply, MessageHelper.PackageResponse(msg));
        }

        private static void GetCustomer(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // create a reply
            var result = new
            {
                reply = "I got the GET message"
            };

            // store the reply on the NATS message
            msg.Response = JsonConvert.SerializeObject(result);

            // send the NATS message (with the response now set) back to the caller
            _connection.Publish(e.Message.Reply, MessageHelper.PackageResponse(msg));
        }

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
                _connection.SubscribeAsync("get.test.v1.customer", "test-microservice-group", GetCustomer),
                _connection.SubscribeAsync("post.test.v1.customer", "test-microservice-group", PostCustomer),
                _connection.SubscribeAsync("put.test.v1.customer", "test-microservice-group", PutCustomer),
                 _connection.SubscribeAsync("delete.test.v1.customer", "test-microservice-group", DeleteCustomer)
            };

            // start the subscriptions
            subscriptions.ForEach(s => s.Start());

            // keep this console app running
            Console.WriteLine($"Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }

        private static void PostCustomer(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // create a reply
            var result = new
            {
                reply = "I got the POST message"
            };

            // store the reply on the NATS message
            msg.Response = JsonConvert.SerializeObject(result);

            // send the NATS message (with the response now set) back to the caller
            _connection.Publish(e.Message.Reply, MessageHelper.PackageResponse(msg));
        }

        private static void PutCustomer(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // create a reply
            var result = new
            {
                reply = "I got the PUT message"
            };

            // store the reply on the NATS message
            msg.Response = JsonConvert.SerializeObject(result);

            // lets also override the response status code from the default 201
            msg.ResponseStatusCode = 200;

            // send the NATS message (with the response now set) back to the caller
            _connection.Publish(e.Message.Reply, MessageHelper.PackageResponse(msg));
        }
    }
}