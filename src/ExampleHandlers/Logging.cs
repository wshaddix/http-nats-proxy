﻿using Newtonsoft.Json;
using Proxy.Shared;
using Serilog;

namespace ExampleHandlers;

public class Logging : IMessageObserver
{
    public Task ObserveAsync(MicroserviceMessage? msg)
    {
        Log.Information("{Message}", JsonConvert.SerializeObject(msg));

        return Task.FromResult(true);
    }
}