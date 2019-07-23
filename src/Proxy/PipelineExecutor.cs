using NATS.Client;
using Newtonsoft.Json;
using Proxy.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    internal class PipelineExecutor
    {
        private readonly Pipeline _incomingPipeline;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IConnection _natsConnection;
        private readonly IEnumerable<string> _observers;
        private readonly Pipeline _outgoingPipeline;
        private readonly int _timeout;

        public PipelineExecutor(IConnection natsConnection,
            JsonSerializerSettings jsonSerializerSettings, int timeout,
            Pipeline incomingPipeline, Pipeline outgoingPipeline,
            IEnumerable<string> observers)
        {
            _natsConnection = natsConnection;
            _jsonSerializerSettings = jsonSerializerSettings;
            _timeout = timeout;
            _incomingPipeline = incomingPipeline;
            _outgoingPipeline = outgoingPipeline;
            _observers = observers;
        }

        internal async Task ExecutePipelineAsync(MicroserviceMessage message)
        {
            // execute the incoming pipeline steps allowing for request termination
            await ExecutePipelineInternalAsync(message, _incomingPipeline).ConfigureAwait(false);

            // execute the outgoing pipeline steps (no request termination)
            await ExecutePipelineInternalAsync(message, _outgoingPipeline, false).ConfigureAwait(false);

            // capture the execution time that it took to process the message
            message.MarkComplete();
        }

        internal void NotifyObservers(MicroserviceMessage message)
        {
            foreach (var observer in _observers)
            {
                // ensure the nats connection is still in a CONNECTED state
                VerifyNatsConnection();

                // send the message to the nats server
                _natsConnection.Publish(observer, message.ToBytes(_jsonSerializerSettings));
            }
        }

        private static MicroserviceMessage ExtractMessageFromReply(Msg reply)
        {
            // the NATS msg.Data property is a json encoded instance of our MicroserviceMessage so we convert it from a byte[] to a string and then
            // deserialize it from json
            return JsonConvert.DeserializeObject<MicroserviceMessage>(Encoding.UTF8.GetString(reply.Data));
        }

        private static void MergeMessageProperties(MicroserviceMessage message, MicroserviceMessage responseMessage)
        {
            // we don't want to lose data on the original message if a microservice fails to return all of the data so we're going to just copy
            // non-null properties from the responseMessage onto the message
            message.ShouldTerminateRequest = responseMessage.ShouldTerminateRequest;
            message.ResponseStatusCode = responseMessage.ResponseStatusCode;
            message.ResponseBody = responseMessage.ResponseBody;
            message.ErrorMessage = responseMessage.ErrorMessage ?? message.ErrorMessage;

            // we want to concatenate the extended properties as each step in the pipeline may be adding information
            responseMessage.ExtendedProperties.ToList().ForEach(h =>
            {
                if (!message.ExtendedProperties.ContainsKey(h.Key))
                {
                    message.ExtendedProperties.Add(h.Key, h.Value);
                }
            });

            // we want to add any request headers that the pipeline step could have added that are not already in the RequestHeaders dictionary
            responseMessage.RequestHeaders.ToList().ForEach(h =>
            {
                if (!message.RequestHeaders.ContainsKey(h.Key))
                {
                    message.RequestHeaders.Add(h.Key, h.Value);
                }
            });

            // we want to add any response headers that the pipeline step could have added that are not already in the ResponseHeaders dictionary
            responseMessage.ResponseHeaders.ToList().ForEach(h =>
            {
                if (!message.ResponseHeaders.ContainsKey(h.Key))
                {
                    message.ResponseHeaders.Add(h.Key, h.Value);
                }
            });
        }

        private async Task ExecutePipelineInternalAsync(MicroserviceMessage message, Pipeline pipeline, bool allowTermination = true)
        {
            var sw = Stopwatch.StartNew();
            foreach (var step in pipeline.Steps)
            {
                // start a timer
                sw.Restart();

                // execute the step
                await ExecuteStepAsync(message, step).ConfigureAwait(false);

                // stop the timer
                sw.Stop();

                // record how long the step took to execute
                message.CallTimings.Add(new CallTiming(step.Subject, sw.ElapsedMilliseconds));

                // if the step requested termination we should stop processing steps
                if (allowTermination && message.ShouldTerminateRequest)
                {
                    break;
                }
            }
        }

        private async Task ExecuteStepAsync(MicroserviceMessage message, Step step)
        {
            // the subject is the step's configured subject unless it is an '*' in which case it's the microservice itself
            var subject = step.Subject.Equals("*") ? message.Subject : step.Subject;

            try
            {
                // if the step pattern is "publish" then do a fire-and-forget NATS call, otherwise to a request/response
                if (step.Pattern.Equals("publish", StringComparison.OrdinalIgnoreCase))
                {
                    // ensure the nats connection is still in a CONNECTED state
                    VerifyNatsConnection();

                    // send the message to the nats server
                    _natsConnection.Publish(subject, message.ToBytes(_jsonSerializerSettings));
                }
                else
                {
                    // ensure the nats connection is still in a CONNECTED state
                    VerifyNatsConnection();

                    // call the step and wait for the response
                    var response = await _natsConnection.RequestAsync(subject, message.ToBytes(_jsonSerializerSettings), _timeout).ConfigureAwait(false);

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

        private void VerifyNatsConnection()
        {
            if (_natsConnection.State != ConnState.CONNECTED)
            {
                throw new Exception(
                    $"Cannot send message to the NATS server because the connection is in a {_natsConnection.State} state");
            }
        }
    }
}