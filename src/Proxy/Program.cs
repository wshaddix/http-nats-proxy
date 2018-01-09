using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NATS.Client;
using Newtonsoft.Json;

namespace Proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // configure which port for Kestrel to listen on
            var port = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_HOST_PORT") ?? "5000";

            // configure the url to the NATS server
            var natsUrl = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_NAT_URL") ?? "nats://localhost:4222";

            // configure how long we are willing to wait for a reply after sending the message to the NATS server
            var timeout = Environment.GetEnvironmentVariable("HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS") ?? "10";

            // create a connection to the NATS server
            var connectionFactory = new ConnectionFactory();
            var connection = connectionFactory.CreateConnection(natsUrl);

            // configure the host
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    // tell Kestrel to listen on all ip addresses at the specififed port
                    options.Listen(IPAddress.Any, int.Parse(port));
                })
                .Configure(app =>
                {
                    // every http request will be handled by this lambda
                    app.Run(async context =>
                    {
                        try
                        {
                            // create a NATS subject from the request method and path
                            var subject = ExtractSubject(context.Request.Method, context.Request.Path.Value);

                            // create the body of the NATS message from the request headers, cookies, query params and body
                            var natsMessage = ExtractMessage(context.Request);

                            // serialize the natsMessage so that we can send it to NATS
                            var serializedMessage = JsonConvert.SerializeObject(natsMessage);

                            // send the message to NATS and wait for a reply
                            var reply = await connection.RequestAsync(subject,
                                Encoding.UTF8.GetBytes(serializedMessage), int.Parse(timeout) * 1000);

                            // convert the response to a string
                            var response = Encoding.UTF8.GetString(reply.Data);

                            // return the response to the api client
                            await context.Response.WriteAsync(response);
                        }
                        catch (Exception ex)
                        {
                            // set the status code to 500 (internal server error)
                            context.Response.StatusCode = 500;

                            // create an anonymous type to hold the error details
                            var response = new
                            {
                                ErrorMessage = ex.GetBaseException().Message
                            };

                            // write the response as a json formatted response
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
                        }
                    });
                })
                .Build();

            // run the host
            host.Run();
        }

        private static NatsMessage ExtractMessage(HttpRequest request)
        {
            string body;

            // if there is a body with the request then read it
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                body = reader.ReadToEnd();
            }

            var natsMessage = new NatsMessage
            {
                Headers = request.Headers.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)),
                Cookies = request.Cookies.Select(c => new KeyValuePair<string, string>(c.Key, c.Value)),
                QueryParams = request.Query.Select(q => new KeyValuePair<string, string>(q.Key, q.Value)),
                Body = body
            };

            return natsMessage;
        }

        private static string ExtractSubject(string method, string path)
        {
            var postfix = path.Replace('/', '.');
            var subject = string.Concat(method, postfix).ToLower();

            return subject;
        }
    }
}