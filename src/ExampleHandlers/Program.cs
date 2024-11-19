using Microsoft.Extensions.DependencyInjection;
using Proxy.Shared;
using Serilog;

namespace ExampleHandlers
{
    internal class Program
    {
        private static readonly ManualResetEvent _mre = new ManualResetEvent(false);
        private static IServiceProvider? _container;

        private static void ConfigureNatsSubscriptions()
        {
            Log.Information("Configuring NATS Subscriptions");

            // create the nats subscription handler
            if (_container is null)
            {
                throw new Exception("container is null");
            }
            var subscriptionHandler = new NatsSubscriptionHandler(_container);
            const string queueGroup = "Example.Queue.Group";

            NatsHelper.Configure(cfg =>
            {
                cfg.ClientName = "Example Message Handlers";
                cfg.NatsServerUrls = ["nats://localhost:4222"];
                cfg.PingInterval = 2000;
                cfg.MaxPingsOut = 2;

                // healthcheck (handler of HTTP - GET /healthcheck)
                cfg.NatsSubscriptions.Add(new NatsSubscription("get.healthcheck", queueGroup, subscriptionHandler.HandleMsgWith<Healthcheck>));

                // tracing (handler as a pipeline step)
                cfg.NatsSubscriptions.Add(new NatsSubscription("add.tracing", queueGroup, subscriptionHandler.HandleMsgWith<Tracing>));

                // metrics (observer as a pipeline step)
                cfg.NatsSubscriptions.Add(new NatsSubscription("record.metrics", queueGroup, subscriptionHandler.ObserveMsgWith<Metrics>));

                // logging (observer as a pipeline step)
                cfg.NatsSubscriptions.Add(new NatsSubscription("logging", queueGroup, subscriptionHandler.ObserveMsgWith<Logging>));

                // customers (handler of HTTP - Get /customers?id=41)
                cfg.NatsSubscriptions.Add(new NatsSubscription("get.customers", queueGroup, subscriptionHandler.HandleMsgWith<GetCustomer>));
            });

            Log.Information("NATS Subscriptions Configured");
        }

        private static void Main(string[] args)
        {
            // setup our logger
            SetupLogger();

            // configure our ioc container
            SetupDependencies();

            // set up an event handler that will run when our application process shuts down
            SetupShutdownHandler();

            // configure our NATS subscriptions that we are going to listen to
            ConfigureNatsSubscriptions();

            // run until we're shutdown
            _mre.WaitOne();
        }

        private static void SetupDependencies()
        {
            // create a new service collection
            var serviceCollection = new ServiceCollection();

            // scan our assembly for all classes that implement IUseCase and register them with a scoped lifetime
            serviceCollection.Scan(scan => scan
                .FromAssembliesOf(typeof(Healthcheck))
                .AddClasses()
                .AsSelf()
                //.AsImplementedInterfaces()
                .WithScopedLifetime());

            // build the IServiceProvider
            _container = serviceCollection.BuildServiceProvider();
        }

        private static void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static void SetupShutdownHandler()
        {
            Console.CancelKeyPress += (_, _) =>
            {
                // log that we are shutting down
                Log.Information("Example Handlers are shutting down");

                // shutdown the logger
                Log.CloseAndFlush();
            };
        }
    }
}