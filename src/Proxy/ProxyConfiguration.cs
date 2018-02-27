using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Proxy
{
    public class ProxyConfiguration
    {
        public JsonSerializerSettings JsonSerializerSettings;
        public string ContentType { get; set; }
        public int DeleteStatusCode { get; set; }
        public int GetStatusCode { get; set; }
        public int HeadStatusCode { get; set; }
        public string Host { get; set; }
        public Pipeline IncomingPipeline { get; private set; }
        public IConnection NatsConnection { get; set; }
        public string NatsUrl { get; set; }
        public IList<string> Observers { get; set; }
        public Pipeline OutgoingPipeline { get; private set; }
        public int PatchStatusCode { get; set; }
        public string PipelineConfigFile { get; set; }
        public string Port { get; set; }
        public int PostStatusCode { get; set; }
        public int PutStatusCode { get; set; }
        public int Timeout { get; set; }

        public ProxyConfiguration()
        {
            Host = Environment.MachineName;

            // configure the json serializer settings to use
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public void Build()
        {
            // build the entire request pipeline
            var pipeline = BuildRequestPipeline();

            // configure the incoming pipeline
            ConfigureIncomingPipeline(pipeline);

            // configure the outgoing pipeline
            ConfigureOutgoingPipeline(pipeline);

            // configure the observers
            ConfigureObservers(pipeline);
        }

        private Pipeline BuildRequestPipeline()
        {
            Pipeline pipeline;

            // if the pipeline config file has been set, read in the configuration.
            if (!string.IsNullOrWhiteSpace(PipelineConfigFile))
            {
                if (!File.Exists(PipelineConfigFile))
                {
                    throw new Exception($"The pipeline config file {PipelineConfigFile} does not exist.");
                }

                var deserializer = new DeserializerBuilder()
                                  .WithNamingConvention(new CamelCaseNamingConvention())
                                  .Build();

                pipeline = deserializer.Deserialize<Pipeline>(File.ReadAllText(PipelineConfigFile));
            }
            else
            {
                // if the pipeline config file has not been set then just send all requests directly to the microservice
                pipeline = new Pipeline
                {
                    Steps = new List<Step>
                                       {
                                           new Step
                                           {
                                               Subject   = "*",
                                               Direction = "incoming",
                                               Order     = 1,
                                               Pattern   = "request"
                                           }
                                       }
                };
            }

            return pipeline;
        }

        private void ConfigureIncomingPipeline(Pipeline pipeline)
        {
            IncomingPipeline = new Pipeline();

            // return just those steps that are to be ran for the directions of "incoming" or "both"
            var directions = new List<string> { "incoming", "both" };

            foreach (var step in pipeline.Steps.OrderBy(s => s.Order))
            {
                if (directions.Contains(step.Direction.ToLower()))
                {
                    // add the step to the incoming pipeline
                    IncomingPipeline.Steps.Add(step);
                }
            }
        }

        private void ConfigureObservers(Pipeline pipeline)
        {
            Observers = new List<string>();

            foreach (var observer in pipeline.Observers)
            {
                Observers.Add(observer.Subject);
            }
        }

        private void ConfigureOutgoingPipeline(Pipeline pipeline)
        {
            OutgoingPipeline = new Pipeline();

            // return just those steps that are to be ran for the directions of "outgoing" or "both"
            var directions = new List<string> { "outgoing", "both" };

            foreach (var step in pipeline.Steps.OrderByDescending(s => s.Order))
            {
                if (directions.Contains(step.Direction.ToLower()))
                {
                    // add the step to the incoming pipeline
                    OutgoingPipeline.Steps.Add(step);
                }
            }
        }
    }
}