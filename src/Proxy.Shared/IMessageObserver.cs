namespace Proxy.Shared
{
    public interface IMessageObserver
    {
        Task ObserveAsync(MicroserviceMessage? msg);
    }
}