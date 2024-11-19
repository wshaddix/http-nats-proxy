using Proxy.Shared;

namespace ExampleHandlers
{
    public class GetCustomer : IMessageHandler
    {
        public async Task<MicroserviceMessage> HandleAsync(MicroserviceMessage? msg)
        {
            if (msg is null)
            {
                throw new Exception("msg is null");
            }

            // grab the customer id that is being fetched
            if (msg.TryGetParam<string>("id", out var customerId))
            {
                // simulate code to go query the database for the customer
                await Task.Delay(1000);

                // create an anonymous type to represent our response
                var reply = new
                {
                    id = customerId,
                    firstName = "John",
                    lastName = "Smith"
                };

                // set the response on the message
                msg.SetResponse(reply);
            }
            else
            {
                // the id parameter was missing
                msg.ErrorMessage = "You must specify the id parameter";
                msg.ResponseStatusCode = 400;
            }

            // return the updated message
            return msg;
        }
    }
}