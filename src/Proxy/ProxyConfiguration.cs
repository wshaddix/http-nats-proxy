using NATS.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Proxy;

public class ProxyConfiguration
{
    public readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public required string ContentType { get; init; }
    public int DeleteStatusCode { get; init; }
    public int GetStatusCode { get; init; }
    public int HeadStatusCode { get; init; }
    public string Host { get; set; } = Environment.MachineName;
    public Pipeline? IncomingPipeline { get; private set; }
    public IConnection? NatsConnection { get; set; }
    public required string NatsUrl { get; init; }
    public IList<string> Observers { get; set; } = new List<string>();
    public Pipeline? OutgoingPipeline { get; private set; }
    public int PatchStatusCode { get; init; }
    public required string PipelineConfigFile { get; init; }
    public required string Port { get; init; }
    public int PostStatusCode { get; init; }
    public int PutStatusCode { get; init; }
    public int Timeout { get; init; }

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
            if (!File.Exists(PipelineConfigFile)) throw new Exception($"The pipeline config file {PipelineConfigFile} does not exist.");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            pipeline = deserializer.Deserialize<Pipeline>(File.ReadAllText(PipelineConfigFile));
        }
        else
        {
            // if the pipeline config file has not been set then just send all requests directly to the microservice
            pipeline = new Pipeline
            {
                Steps =
                [
                    new Step
                    {
                        Subject = "*",
                        Direction = "incoming",
                        Order = 1,
                        Pattern = "request"
                    }
                ]
            };
        }

        return pipeline;
    }

    private void ConfigureIncomingPipeline(Pipeline pipeline)
    {
        IncomingPipeline = new Pipeline();

        // return just those steps that are to be run for the directions of "incoming" or "both"
        var directions = new List<string> { "incoming", "both" };

        foreach (var step in pipeline.Steps.OrderBy(s => s.Order))
            if (directions.Contains(step.Direction.ToLower()))
                // add the step to the incoming pipeline
                IncomingPipeline.Steps.Add(step);
    }

    private void ConfigureObservers(Pipeline pipeline)
    {
        Observers = new List<string>();

        foreach (var observer in pipeline.Observers) Observers.Add(observer.Subject);
    }

    private void ConfigureOutgoingPipeline(Pipeline pipeline)
    {
        OutgoingPipeline = new Pipeline();

        // return just those steps that are to be ran for the directions of "outgoing" or "both"
        var directions = new List<string> { "outgoing", "both" };

        foreach (var step in pipeline.Steps.OrderByDescending(s => s.Order))
            if (directions.Contains(step.Direction.ToLower()))
                // add the step to the incoming pipeline
                OutgoingPipeline.Steps.Add(step);
    }
}