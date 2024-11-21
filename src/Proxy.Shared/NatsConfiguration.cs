namespace Proxy.Shared;

public class NatsConfiguration
{
    public string ClientName { get; set; } = "N/A";
    public int MaxPingsOut { get; set; } = 2;
    public required string[] NatsServerUrls { get; set; } = ["nats://localhost:4222"];
    public List<NatsSubscription> NatsSubscriptions { get; set; } = [];
    public int PingInterval { get; set; } = 2000;

    internal void Validate()
    {
        // if the nats server url isn't specified throw an exception
        if (null == NatsServerUrls || NatsServerUrls.Length == 0) throw new ArgumentNullException(nameof(NatsServerUrls));

        // if the client name isn't specified throw an exception
        if (string.IsNullOrWhiteSpace(ClientName)) throw new ArgumentNullException(nameof(ClientName));

        // if there are no subscriptions specified throw an exception
        if (NatsSubscriptions.Count == 0) throw new ArgumentException("You must have at least one subscription specified.");
    }
}