using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    public class RequestHandler
    {
        private readonly ProxyConfig _config;

        public RequestHandler(ProxyConfig config)
        {
            _config = config;
        }

        public async Task HandleAsync(HttpContext context)
        {
            NatsMessage natsMessage = null;
            Stopwatch stopwatch = null;

            try
            {
                // if we're capturing metrics then we need to time the entire pipeline
                if (_config.PublishMetrics)
                {
                    stopwatch = Stopwatch.StartNew();
                }

                // inject a trace header if we are configured to do so
                ProcessTraceHeader(context);

                // create a NATS subject from the request method and path
                var subject = ExtractSubject(context.Request.Method, context.Request.Path.Value);

                // create the body of the NATS message from the request headers, cookies, query params and body
                natsMessage = ExtractMessage(context.Request);

                // add metadata to the nats message for logging purposes
                natsMessage.Host = _config.Host;
                natsMessage.Subject = subject;
                context.Response.ContentType = _config.ContentType;
                natsMessage.ResponseContentType = _config.ContentType;

                // send the message to NATS and wait for a reply
                var reply = await _config.NatsConnection.RequestAsync(subject, natsMessage.ToBytes(_config.JsonSerializerSettings), _config.Timeout);

                // convert the response to a string
                var response = Encoding.UTF8.GetString(reply.Data);

                // add the response to the natsMessage for logging
                natsMessage.Response = response;

                // set the response status code
                var statusCode = DetermineStatusCode(context);
                context.Response.StatusCode = statusCode;
                natsMessage.ResponseStatusCode = statusCode;

                // return the response to the api client
                await context.Response.WriteAsync(response);

                // if we're capturing metrics we need to get the ellapsed time and publish it
                if (_config.PublishMetrics)
                {
                    PublishTimingMetric(stopwatch, natsMessage, subject);
                }

                // if we're logging then publish the completed natsMessage
                if (_config.PublishLogs)
                {
                    LogNatsMessage(natsMessage);
                }
            }
            catch (Exception ex)
            {
                // set the status code to 500 (internal server error)
                context.Response.StatusCode = 500;
                natsMessage.ResponseStatusCode = 500;
                natsMessage.ErrorMessage = ex.GetBaseException().Message;

                // create an anonymous type to hold the error details
                var response = new
                {
                    ErrorMessage = natsMessage.ErrorMessage
                };

                // write the response as a json formatted response
                await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _config.JsonSerializerSettings));

                // if we're logging then publish the completed natsMessage
                if (_config.PublishLogs)
                {
                    LogNatsMessage(natsMessage);
                }
            }
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

        private void LogNatsMessage(NatsMessage natsMessage)
        {
            _config.NatsConnection.Publish(_config.LogsSubject, natsMessage.ToBytes(_config.JsonSerializerSettings));
        }

        private void ProcessTraceHeader(HttpContext context)
        {
            if (_config.AddTraceHeader)
            {
                // add the trace header only if it does not already exist
                if (!context.Request.Headers.ContainsKey(_config.TraceHeaderName))
                {
                    context.Request.Headers.Add(new KeyValuePair<string, StringValues>(_config.TraceHeaderName,
                                                                                       Guid.NewGuid().ToString("N")));
                }
            }
        }

        private void PublishTimingMetric(Stopwatch stopwatch, NatsMessage natsMessage, string subject)
        {
            stopwatch?.Stop();
            var timeMs = stopwatch?.ElapsedMilliseconds;

            // add the call time to the natsMessage for logging purposes
            natsMessage.ExecutionTimeMs = timeMs;

            var metrics = new
            {
                subject = subject,
                executionTimeMs = timeMs,
                occurredAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            var metricsMsg = JsonConvert.SerializeObject(metrics, _config.JsonSerializerSettings);
            _config.NatsConnection.Publish(_config.MetricsSubject, Encoding.UTF8.GetBytes(metricsMsg));
        }
    }
}