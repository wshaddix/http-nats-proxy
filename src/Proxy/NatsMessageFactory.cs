using Proxy.Shared;

namespace Proxy
{
    internal static class NatsMessageFactory
    {
        internal static MicroserviceMessage InitializeMessage(ProxyConfiguration config)
        {
            return new MicroserviceMessage(config.Host, config.ContentType);
        }
    }
}