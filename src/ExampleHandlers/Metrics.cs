using Proxy.Shared;
using Serilog;
using System.Threading.Tasks;

namespace ExampleHandlers
{
    public class Metrics : IMessageObserver
    {
        public Task ObserveAsync(MicroserviceMessage msg)
        {
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