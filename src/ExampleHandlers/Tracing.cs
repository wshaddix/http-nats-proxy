using Proxy.Shared;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ExampleHandlers
{
    public class Tracing : IMessageHandler
    {
        public Task<MicroserviceMessage> HandleAsync(MicroserviceMessage msg)
        {
            const string headerName = "x-trace-id";

            // see if a trace header is already on the msg
            if (msg.TryGetParam<string>(headerName, out var traceHeader))
            {
                Log.Information("Message already had trace id of {TraceId} present", traceHeader);
            }
            else
            {
                // add a trace header to the msg
                var traceId = Guid.NewGuid().ToString("N");
                msg.RequestHeaders.Add(headerName, traceId);

                Log.Information("Added trace id of {TraceId} to the message", traceId);
            }

            return Task.FromResult(msg);
        }
    }
}