using Proxy.Shared;

namespace ExampleHandlers
{
    public class Healthcheck : IMessageHandler
    {
        async Task<MicroserviceMessage> IMessageHandler.HandleAsync(MicroserviceMessage? msg)
        {
            if (msg is null)
            {
                throw new Exception("msg is null");
            }

            // simulate code to go check connections to infrastructure dependencies like a database, redis cache, 3rd party api, etc
            await Task.Delay(1000);

            // create an anonymous type to represent our response
            var reply = new
            {
                status = "ok"
            };

            // set the response on the message
            msg.SetResponse(reply);

            // return the updated message
            return msg;
        }
    }
}