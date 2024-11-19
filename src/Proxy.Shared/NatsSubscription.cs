using NATS.Client;

namespace Proxy.Shared
{
    public class NatsSubscription
    {
        public EventHandler<MsgHandlerEventArgs> Handler { get; private set; }
        public string QueueGroup { get; private set; }
        public string Subject { get; private set; }

        public NatsSubscription(string subject, string queueGroup, EventHandler<MsgHandlerEventArgs> handler)
        {
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            QueueGroup = queueGroup ?? throw new ArgumentNullException(nameof(queueGroup));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}