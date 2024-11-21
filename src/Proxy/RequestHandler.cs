using Newtonsoft.Json;
using Proxy.Shared;

namespace Proxy;

public class RequestHandler
{
    private readonly ProxyConfiguration _config;
    private readonly PipelineExecutor _pipelineExecutor;

    public RequestHandler(ProxyConfiguration config)
    {
        _config = config;

        Guard.AgainstNull(_config.NatsConnection);
        Guard.AgainstNull(_config.IncomingPipeline);
        Guard.AgainstNull(_config.OutgoingPipeline);

        _pipelineExecutor = new PipelineExecutor(_config.NatsConnection!,
            _config.JsonSerializerSettings,
            _config.Timeout,
            _config.IncomingPipeline!,
            _config.OutgoingPipeline!,
            _config.Observers);
    }

    public async Task HandleAsync(HttpContext context)
    {
        var message = NatsMessageFactory.InitializeMessage(_config);

        try
        {
            // create a nats message from the http request
            await CreateNatsMsgFromHttpRequestAsync(context.Request, message);

            // execute the request pipeline
            await _pipelineExecutor.ExecutePipelineAsync(message).ConfigureAwait(false);

            // create the http response from the processed nats message
            CreateHttpResponseFromNatsMsg(context, message);

            // notify any observers that want a copy of the completed request/response
            _pipelineExecutor.NotifyObservers(message);
        }
        catch (Exception ex)
        {
            // set the status code to 500 (internal server error)
            context.Response.StatusCode = 500;
            message.ResponseStatusCode = 500;
            message.ErrorMessage = ex.GetBaseException().Message;

            object response;

            if (ex is StepException stepException)
                response = new
                {
                    stepException.Subject,
                    stepException.Pattern,
                    Message = stepException.Msg
                };
            else
                response = new
                {
                    message.ErrorMessage
                };

            // capture the execution time that it took to process the message
            message.MarkComplete();

            // write the response as a json formatted response
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _config.JsonSerializerSettings)).ConfigureAwait(false);
        }
    }

    private static async Task CreateNatsMsgFromHttpRequestAsync(HttpRequest httpRequest, MicroserviceMessage message)
    {
        // create a NATS subject from the request method and path
        if (httpRequest.Path.Value is null) throw new Exception("httpRequest.Path.Value is null");

        message.Subject = NatsSubjectParser.Parse(httpRequest.Method, httpRequest.Path.Value);

        // parse the request headers, cookies, query params and body and put them on the message
        await ParseHttpRequestAsync(httpRequest, message);
    }

    private static async Task ParseHttpRequestAsync(HttpRequest request, MicroserviceMessage message)
    {
        // parse the http request body
        message.RequestBody = await HttpRequestParser.ParseBodyAsync(request.Body);

        // parse the request headers
        message.RequestHeaders = HttpRequestParser.ParseHeaders(request.Headers);

        // parse the cookies
        message.Cookies = HttpRequestParser.ParseCookies(request.Cookies);

        // parse the query string parameters
        message.QueryParams = HttpRequestParser.ParseQueryParams(request.Query);
    }

    private void CreateHttpResponseFromNatsMsg(HttpContext context, MicroserviceMessage message)
    {
        // set the response status code
        message.ResponseStatusCode =
            message.ResponseStatusCode == -1 ? DetermineStatusCode(context) : message.ResponseStatusCode;

        // build up the http response
        HttpResponseFactory.PrepareResponseAsync(context.Response, message, _config.JsonSerializerSettings);
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
}