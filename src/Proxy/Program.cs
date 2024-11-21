using System.Net;
using NATS.Client;
using Proxy.Shared;
using Serilog;
using Serilog.Events;

namespace Proxy;

public class Program
{
    private static ProxyConfiguration? _config;
    private static IWebHost? _host;

    public static void Main(string[] args)
    {
        // configure our logger
        ConfigureLogging();

        // capture the runtime configuration settings
        ConfigureEnvironment();

        // create a connection to the NATS server
        ConnectToNats();

        // configure the host
        ConfigureWebHost();

        // run the host
        _host?.Run();
    }

    private static void ConfigureEnvironment()
    {
        Log.Information("Reading configuration values");

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

        Log.Information("Configured");
    }

    private static void ConfigureLogging()
    {
        var logProperties = Environment.GetEnvironmentVariable("LOGGING_PROPERTIES") ?? string.Empty;
        var logLevelSetting = Environment.GetEnvironmentVariable("LOGGING_LEVEL") ?? "Verbose";

        // set the configured log level
        if (!Enum.TryParse<LogEventLevel>(logLevelSetting, true, out var logLevel)) logLevel = LogEventLevel.Verbose;

        // create the logging configuration
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console();

        // set any log properties that are configured
        // these should be in the format of property=value,property=value,property=value
        if (!string.IsNullOrWhiteSpace(logProperties))
        {
            // split the properties by comma
            var properties = logProperties.Split(',');

            foreach (var property in properties)
            {
                // add the property and value to the logging configuration
                var key = property.Split('=')[0];
                var value = property.Split('=')[1];
                loggerConfiguration.Enrich.WithProperty(key, value);
            }
        }

        // create the logger
        Log.Logger = loggerConfiguration.CreateLogger();
    }

    private static void ConfigureWebHost()
    {
        Guard.AgainstNull(_config);

        // create the request handler
        var requestHandler = new RequestHandler(_config!);

        _host = new WebHostBuilder()
            .UseKestrel(options =>
            {
                // tell Kestrel to listen on all ip addresses at the specified port
                options.Listen(IPAddress.Any, int.Parse(_config!.Port));
            })
            .ConfigureServices(services =>
            {
                // enable cors
                services.AddCors(options =>
                {
                    options.AddPolicy("DefaultCORS", builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowAnyOrigin();
                        //.AllowCredentials();
                    });
                });
            })
            .Configure(app =>
            {
                // configure cors to allow any origin
                app.UseCors("DefaultCORS");

                // every http request will be handled by our request handler
                app.Run(requestHandler.HandleAsync);
            })
            .Build();
    }

    private static void ConnectToNats()
    {
        Guard.AgainstNull(_config);

        Log.Information("Attempting to connect to NATS server at: {NatsUrl}", _config!.NatsUrl);

        // create a connection to the NATS server
        var connectionFactory = new ConnectionFactory();
        var options = ConnectionFactory.GetDefaultOptions();
        options.AllowReconnect = true;
        options.Url = _config.NatsUrl;
        options.PingInterval = 1000;
        options.MaxPingsOut = 2;
        options.AsyncErrorEventHandler += (sender, args) => { Log.Information("The AsyncErrorEvent was just handled"); };
        options.ClosedEventHandler += (sender, args) => { Log.Information("The ClosedEvent was just handled"); };
        options.DisconnectedEventHandler += (sender, args) => { Log.Information("The DisconnectedEvent was just handled"); };
        options.ReconnectedEventHandler += (sender, args) => { Log.Information("The ReconnectedEvent was just handled"); };
        options.ServerDiscoveredEventHandler += (sender, args) => { Log.Information("The ServerDiscoveredEvent was just handled"); };
        options.Name = "http-nats-proxy";
        _config.NatsConnection = connectionFactory.CreateConnection(options);

        Log.Information("HttpNatsProxy connected to NATS at {Url}", options.Url);
        Log.Information("Waiting for messages");
    }
}