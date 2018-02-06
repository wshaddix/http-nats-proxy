using NATS.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TraceHeaderPipelineStep
{
    internal class Program
    {
        // we use a mre to keep the console application running while it's waiting on messages from NATS in the background
        private static readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        private static readonly string TraceHeaderName = Environment.GetEnvironmentVariable("TRACE_HEADER_NAME") ?? "x-trace-id";
        private static IConnection _connection;

        private static void InjectTraceHeader(object sender, MsgHandlerEventArgs e)
        {
            // deserialize the NATS message
            var msg = MessageHelper.GetNatsMessage(e.Message);

            // if the msg doesn't include a trace header we need to inject one
            var traceHeader = msg.RequestHeaders.FirstOrDefault(h => h.Key.Equals(TraceHeaderName));

            if (null == traceHeader.Value)
            {
                var traceId = Guid.NewGuid().ToString("N");
                msg.RequestHeaders.Add(new KeyValuePair<string, string>(TraceHeaderName, traceId));
                Console.WriteLine($"Added trace header name: {TraceHeaderName} value: {traceId}");
            }
            else
            {
                Console.WriteLine($"TraceId of {traceHeader.Value} was already on the message so nothing to do.");
            }

            // send the NATS message (with the trace header now set) back to the caller
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
                _connection.SubscribeAsync("trace.header", "trace-header-microservice-group", InjectTraceHeader),
            };

            // start the subscriptions
            subscriptions.ForEach(s => s.Start());

            // keep this console app running
            Console.WriteLine($"Trace Header Pipeline Step Connected to NATS at: {natsUrl}\r\nWaiting for messages...");
            ManualResetEvent.WaitOne();
        }
    }
}