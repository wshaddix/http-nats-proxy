using NATS.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AuthenticationPipelineStep
{
    internal class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        private static IConnection _connection;

        private static void EnsureAuthHeaderPresent(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // if the msg doesn't include an authentication header we need to return a redirect to the auth url
            var authHeader = msg.RequestHeaders.FirstOrDefault(h => h.Key.Equals("Authorization"));

            if (null == authHeader.Value)
            {
                Console.WriteLine($"No Authorization header found. Returning a 301 redirect to http://google.com");
                msg.ResponseStatusCode = 301;
                msg.ResponseHeaders.Add(new KeyValuePair<string, string>("Location", "https://google.com"));
                msg.ShouldTerminateRequest = true;
            }
            else
            {
                Console.WriteLine($"Authorization header was already on the message so nothing to do.");
            }

            // send the NATS message back to the caller
            _connection.Publish(e.Message.Reply, MessageHelper.PackageResponse(msg));
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
    }
}