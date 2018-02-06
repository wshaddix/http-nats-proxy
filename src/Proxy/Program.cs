using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                .Configure(app =>
                {
                    // every http request will be handled by our request handler
                    app.Run(requestHandler.HandleAsync);
                })
                .Build();
        }

        private static void ConnectToNats()
        {
            Console.WriteLine($"Attempting to connect to NATS server at: {_config.NatsUrl}");

            var connectionFactory = new ConnectionFactory();
            _config.NatsConnection = connectionFactory.CreateConnection(_config.NatsUrl);

            Console.WriteLine("Connected to NATS server.");
        }
    }
}