using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using System;
using System.Net;

namespace Proxy
{
    public class Program
    {
        private static ProxyConfiguration _config;
        private static IWebHost _host;

        public static void Main(string[] args)
        {
            // capture the runtime configuration settings
            ConfigureEnvironment();

            // create a connection to the NATS server
            ConnectToNats();

            // configure the host
            ConfigureWebHost();

            // run the host
            _host.Run();
        }

        private static void ConfigureEnvironment()
        {
            Console.WriteLine("Reading configuration values...");

            _config = new ProxyConfiguration
            {
                // configure which port for Kestrel to listen on
                Port = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_HOST_PORT") ?? "5000",

                // configure the url to the NATS server
                NatsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222",

                // configure how long we are willing to wait for a reply after sending the message to the NATS server
                Timeout = 1000 * int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS") ?? "10"),

                // configure the http response status codes
                HeadStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_HEAD_STATUS_CODE") ?? "200"),
                PutStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_PUT_STATUS_CODE") ?? "201"),
                GetStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_GET_STATUS_CODE") ?? "200"),
                PatchStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_PATCH_STATUS_CODE") ?? "201"),
                PostStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_POST_STATUS_CODE") ?? "201"),
                DeleteStatusCode = int.Parse(Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_DELETE_STATUS_CODE") ?? "204"),

                // configure the content type of the http response to be used
                ContentType = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_CONTENT_TYPE") ?? "application/json; charset=utf-8",

                // capture the request pipeline config file
                PipelineConfigFile = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_REQUEST_PIPELINE_CONFIG_FILE") ?? string.Empty
            };

            _config.Build();

            Console.WriteLine("Configured.");
        }

        private static void ConfigureWebHost()
        {
            // create the request handler
            var requestHandler = new RequestHandler(_config);

            _host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // tell Kestrel to listen on all ip addresses at the specififed port
                    options.Listen(IPAddress.Any, int.Parse(_config.Port));
                })
                .ConfigureServices(services =>
                {
                    // enable cors
                    services.AddCors();
                })
                .Configure(app =>
                {
                    // configure cors to allow any origin
                    app.UseCors(builder => builder.AllowAnyOrigin());

                    // every http request will be handled by our request handler
                    app.Run(requestHandler.HandleAsync);
                })
                .Build();
        }

        private static void ConnectToNats()
        {
            Console.WriteLine($"Attempting to connect to NATS server at: {_config.NatsUrl}");

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            var options = ConnectionFactory.GetDefaultOptions();
            options.AllowReconnect = true;
            options.Url = _config.NatsUrl;
            options.PingInterval = 1000;
            options.MaxPingsOut = 2;
            options.AsyncErrorEventHandler += (sender, args) =>
            {
                Console.WriteLine("The AsyncErrorEvent was just handled.");
            };
            options.ClosedEventHandler += (sender, args) =>
            {
                Console.WriteLine("The ClosedEvent was just handled.");
            };
            options.DisconnectedEventHandler += (sender, args) =>
            {
                Console.WriteLine("The DisconnectedEvent was just handled.");
            };
            options.ReconnectedEventHandler += (sender, args) =>
            {
                Console.WriteLine("The ReconnectedEvent was just handled.");
            };
            options.ServerDiscoveredEventHandler += (sender, args) =>
            {
                Console.WriteLine("The ServerDiscoveredEvent was just handled.");
            };
            options.Name = "http-nats-proxy";
            _config.NatsConnection = connectionFactory.CreateConnection(options);

            Console.WriteLine($"HttpNatsProxy connected to NATS at: {options.Url}\r\nWaiting for messages...");
        }
    }
}