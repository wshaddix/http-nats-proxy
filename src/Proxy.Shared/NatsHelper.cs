using NATS.Client;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace Proxy.Shared
{
    public static class NatsHelper
    {
        private static IConnection? _connection;

        public static void Configure(Action<NatsConfiguration> configAction)
        {
            // create a new instance of a configuration
            var config = new NatsConfiguration
            {
                NatsServerUrls = []
            };

            // allow the client to set up the subscriptions, connection name and nats server url
            configAction(config);

            // validate the configuration in case there are any problems
            config.Validate();

            // ensure we are connected to the nats server
            Connect(config.ClientName, config.NatsServerUrls, config.PingInterval, config.MaxPingsOut);

            // start the subscriptions
            if (_connection is null)
            {
                throw new InvalidOperationException("The NATS connection is null");
            }
            
            config.NatsSubscriptions.ForEach(s => _connection.SubscribeAsync(s.Subject, s.QueueGroup, s.Handler).Start());
        }

        public static void Publish(MicroserviceMessage? message)
        {
            ArgumentNullException.ThrowIfNull(message);

            Publish(message.Subject, message);
        }

        public static void Publish(string? subject, MicroserviceMessage? message)
        {
            // validate params
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            ArgumentNullException.ThrowIfNull(message);

            // serialize the message
            var serializedMessage = JsonConvert.SerializeObject(message);

            // send the message
            if (_connection is null)
            {
                throw new InvalidOperationException("The NATS connection is null");
            }

            _connection.Publish(subject, Encoding.UTF8.GetBytes(serializedMessage));
        }

        public static Task<MicroserviceMessage?> RequestAsync(MicroserviceMessage message)
        {
            return RequestAsync(message.Subject, message);
        }

        private static async Task<MicroserviceMessage?> RequestAsync(string? subject, MicroserviceMessage message)
        {
            // validate params
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            ArgumentNullException.ThrowIfNull(message);

            // serialize the message
            var serializedMessage = JsonConvert.SerializeObject(message);

            // send the message
            if (_connection is null)
            {
                throw new InvalidOperationException("The NATS connection is null");
            }

            var response = await _connection.RequestAsync(subject, Encoding.UTF8.GetBytes(serializedMessage)).ConfigureAwait(false);

            // return the response as a Microservice message
            return response.Data.ToMicroserviceMessage();
        }

        private static void Connect(string clientName, string[] natsServerUrls, int pingInterval, int maxPingsOut)
        {
            // the params are validated in the Configure method so we don't need to revalidate here

            // if we're already connected to the nats server then do nothing
            if (_connection is { State: ConnState.CONNECTED })
            {
                return;
            }

            // configure the options for this connection
            var connectionFactory = new ConnectionFactory();
            var options = ConnectionFactory.GetDefaultOptions();
            options.Name = clientName;
            options.AllowReconnect = true;
            options.Servers = natsServerUrls;
            options.PingInterval = pingInterval;
            options.MaxPingsOut = maxPingsOut;

            options.AsyncErrorEventHandler += (_, _) =>
            {
                Log.Information("The AsyncErrorEvent was just handled");
            };
            options.ClosedEventHandler += (_, _) =>
            {
                Log.Information("The ClosedEvent was just handled");
            };
            options.DisconnectedEventHandler += (_, _) =>
            {
                Log.Information("The DisconnectedEvent was just handled");
            };
            options.ReconnectedEventHandler += (_, _) =>
            {
                Log.Information("The ReconnectedEvent was just handled");
            };
            options.ServerDiscoveredEventHandler += (_, _) =>
            {
                Log.Information("The ServerDiscoveredEvent was just handled");
            };

            // create a connection to the NATS server
            _connection = connectionFactory.CreateConnection(options);

            Log.Information("Connected to NATS server");
        }
    }
}