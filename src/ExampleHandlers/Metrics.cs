using Proxy.Shared;
using Serilog;

namespace ExampleHandlers
{
    public class Metrics : IMessageObserver
    {
        public Task ObserveAsync(MicroserviceMessage? msg)
        {
            if (msg is null)
            {
                throw new Exception("msg is null");
            }

            // grab all the metrics from the message and "record" them
            foreach (var callTiming in msg.CallTimings)
            {
                Log.Information("Subject: {Subject} - Execution Time (ms): {EllapsedMs}",
                    callTiming.Subject, callTiming.EllapsedMs);
            }

            return Task.FromResult(true);
        }
    }
}