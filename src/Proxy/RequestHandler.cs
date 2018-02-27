using Microsoft.AspNetCore.Http;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    public class RequestHandler
    {
        private readonly ProxyConfiguration _config;

        public RequestHandler(ProxyConfiguration config)
        {
            _config = config;
        }

        public async Task HandleAsync(HttpContext context)
        {
            // create a new message
            var message = new NatsMessage(_config.Host, _config.ContentType);

            try
            {
                // set the content type of the http response
                context.Response.ContentType = _config.ContentType;

                // create a NATS subject from the request method and path
                message.Subject = ExtractSubject(context.Request.Method, context.Request.Path.Value);

                // emit a log message
                Console.WriteLine($"Routing {string.Join(context.Request.Method, "", context.Request.Path.Value)} to {message.Subject}");

                // parse the request headers, cookies, query params and body and put them on the message
                ParseHttpRequest(context.Request, message);

                // execute the incoming request pipeline in order
                var sw = Stopwatch.StartNew();
                foreach (var step in _config.IncomingPipeline.Steps)
                {
                    // execute the step
                    sw.Restart();
                    await ExecuteStep(message, step);
                    sw.Stop();

                    // record how long the step took to execute
                    message.CallTimings.Add((step.Subject, step.Pattern, sw.ElapsedMilliseconds));

                    // if the step requested termination we should stop processing steps
                    if (message.ShouldTerminateRequest)
                    {
                        break;
                    }
                }

                // execute the outgoing request pipeline in descending order
                foreach (var step in _config.OutgoingPipeline.Steps)
                {
                    sw.Restart();
                    await ExecuteStep(message, step);
                    sw.Stop();

                    // record how long the step took to execute
                    message.CallTimings.Add((step.Subject, step.Pattern, sw.ElapsedMilliseconds));
                }

                // set the response status code
                context.Response.StatusCode = message.ResponseStatusCode == -1 ? DetermineStatusCode(context) : message.ResponseStatusCode;

                // set any response headers
                foreach (var header in message.ResponseHeaders)
                {
                    context.Response.GetTypedHeaders().Append(header.Key, header.Value);
                }

                // capture the execution time that it took to process the message
                message.MarkComplete();

                // if the response message includes an error, then return it
                if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
                {
                    var response = new
                    {
                        message.ErrorMessage
                    };

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _config.JsonSerializerSettings));
                }

                // return the response to the api client
                if (!string.IsNullOrWhiteSpace(message.Response))
                {
                    await context.Response.WriteAsync(message.Response);
                }
            }
            catch (Exception ex)
            {
                // set the status code to 500 (internal server error)
                context.Response.StatusCode = 500;
                message.ResponseStatusCode = 500;
                message.ErrorMessage = ex.GetBaseException().Message;

                object response;

                if (ex is StepException stepException)
                {
                    response = new
                    {
                        stepException.Subject,
                        stepException.Pattern,
                        Message = stepException.Msg
                    };
                }
                else
                {
                    response = new
                    {
                        message.ErrorMessage
                    };
                }

                // capture the execution time that it took to process the message
                message.MarkComplete();

                // write the response as a json formatted response
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _config.JsonSerializerSettings));
            }
        }

        private static NatsMessage ExtractMessageFromReply(Msg reply)
        {
            // the NATS msg.Data property is a json encoded instance of our NatsMessage so we convert it from a byte[] to a string and then deserialize
            // it from json
            return JsonConvert.DeserializeObject<NatsMessage>(Encoding.UTF8.GetString(reply.Data));
        }

        private static string ExtractSubject(string method, string path)
        {
            // replace all forward slashes with periods in the http request path
            var subjectPath = path.Replace('/', '.');

            // the subject is the http method followed by the path all lowercased
            return string.Concat(method, subjectPath).ToLower();
        }

        private static NatsMessage MergeMessageProperties(NatsMessage message, NatsMessage responseMessage)
        {
            // we don't want to lose data on the original message if a microservice fails to return all of the data so we're going to just copy
            // non-null properties from the responseMessage onto the message
            message.ShouldTerminateRequest = responseMessage.ShouldTerminateRequest;
            message.ResponseStatusCode = responseMessage.ResponseStatusCode;
            message.Response = responseMessage.Response;
            message.ErrorMessage = responseMessage.ErrorMessage ?? message.ErrorMessage;

            // we want to concatenate the extended properties as each step in the pipeline may be adding information
            message.ExtendedProperties = message.ExtendedProperties
                                           .Concat(responseMessage.ExtendedProperties)
                                           .ToDictionary(e => e.Key, e => e.Value);

            // we want to add any request headers that the pipeline step could have added that are not already in the RequestHeaders dictionary
            responseMessage.RequestHeaders.ToList().ForEach(h =>
            {
                if (!message.RequestHeaders.ContainsKey(h.Key))
                {
                    message.RequestHeaders[h.Key] = h.Value;
                }
            });

            // we want to add any response headers that the pipeline step could have added that are not already in the ResponseHeaders dictionary
            responseMessage.ResponseHeaders.ToList().ForEach(h =>
            {
                if (!message.ResponseHeaders.ContainsKey(h.Key))
                {
                    message.ResponseHeaders[h.Key] = h.Value;
                }
            });

            // return the merged message
            return message;
        }

        private static void ParseHttpRequest(HttpRequest request, NatsMessage message)
        {
            // if there is a body with the request then read it
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                message.Body = reader.ReadToEnd();
            }

            // parse the headers
            foreach (var header in request.Headers)
            {
                message.RequestHeaders[header.Key] = header.Value;
            }

            // parse the cookies
            foreach (var cookie in request.Cookies)
            {
                message.Cookies[cookie.Key] = cookie.Value;
            }

            // parse the query string parameters
            foreach (var param in request.Query)
            {
                message.QueryParams[param.Key] = param.Value;
            }
        }

        private int DetermineStatusCode(HttpContext context)
        {
            var statusCode = 200;
            switch (context.Request.Method.ToLower())
            {
                case "head":
                    statusCode = _config.HeadStatusCode;
                    break;

                case "get":
                    statusCode = _config.GetStatusCode;
                    break;

                case "put":
                    statusCode = _config.PutStatusCode;
                    break;

                case "patch":
                    statusCode = _config.PatchStatusCode;
                    break;

                case "post":
                    statusCode = _config.PostStatusCode;
                    break;

                case "delete":
                    statusCode = _config.DeleteStatusCode;
                    break;
            }

            return statusCode;
        }

        private async Task ExecuteStep(NatsMessage message, Step step)
        {
            // the subject is the step's configured subject unless it is an '*' in which case it's the microservice itself
            var subject = step.Subject.Equals("*") ? message.Subject : step.Subject;

            try
            {
                // if the step pattern is "publish" then do a fire-and-forget NATS call, otherwise to a request/response
                if (step.Pattern.Equals("publish", StringComparison.OrdinalIgnoreCase))
                {
                    // ensure the nats connection is still in a CONNECTED state
                    if (_config.NatsConnection.State != ConnState.CONNECTED)
                    {
                        throw new Exception($"Cannot send message to the NATS server because the connection is in a {_config.NatsConnection.State} state");
                    }

                    // send the message to the nats server
                    _config.NatsConnection.Publish(subject, message.ToBytes(_config.JsonSerializerSettings));
                }
                else
                {
                    // ensure the nats connection is still in a CONNECTED state
                    if (_config.NatsConnection.State != ConnState.CONNECTED)
                    {
                        throw new Exception($"Cannot send message to the NATS server because the connection is in a {_config.NatsConnection.State} state");
                    }

                    // call the step and wait for the response
                    var response = await _config.NatsConnection.RequestAsync(subject, message.ToBytes(_config.JsonSerializerSettings), _config.Timeout);

                    // extract the response message
                    var responseMessage = ExtractMessageFromReply(response);

                    // merge the response into our original nats message
                    MergeMessageProperties(message, responseMessage);
                }
            }
            catch (Exception ex)
            {
                throw new StepException(subject, step.Pattern, ex.GetBaseException().Message);
            }
        }
    }
}