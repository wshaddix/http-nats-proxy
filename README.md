# http-nats-proxy

A proxy service that converts http requests to [NATS](https://nats.io) messages.

## Purpose

This proxy service is designed to be ran in a docker container and will translate all of your http based api calls (REST
endpoints). It will translate the http route to a [NATS](https://nats.io) message and will send the message as a request
into the NATS messaging system. Once a reply has been returned it is written back to the http response. The primary use
case for this service is when you have an ecosystem of microservices that communicate via NATS but you want http REST
api clients to be able to communicate to those services as a typical http REST api client.

## Features

### Routing

When an http request is received the http-nats-proxy will take the request path and replace every forward slash ('/')
with a period ('.'). It will then prefix that with the http verb that was used (get, put, post, delete, head, etc.).
Finally it will convert the entire string to lowercase. This becomes your NATS subject where the message will be sent.
For example, the http request of `GET http://localhost:5000/test/v1/customer?id=1234&show-history=true` will result in a
NATS message subject of `get.test.v1.customer`

### HTTP Request Format Translation

When an http request is received, the http-nats-proxy will extract the headers, cookies, query string parameters and the
body of the request and create a NATS message. The NATS message that goes into the messaging system will be a json
serialized object that has the following structure:

```
{
    "cookies": {
        "key1": "value1",
        "key2": "value2"
    },
    "errorMessage", "",                     <--- can be set by microservices and pipeline steps
    "extendedProperties": {                 <--- can be set by microservices and pipeline steps
        "key1": "value1",
        "key2": "value2"
    },
    "queryParams": {
        "key1": "value1",
        "key2": "value2"
    },
    "requestBody": "",
    "requestHeaders": {
        "key1": "value1",
        "key2": "value2"
    },
    "responseBody": "",                     <--- can be set by microservices and pipeline steps
    "responseHeaders": {                    <--- can be set by microservices and pipeline steps
        "key1": "value1",
        "key2": "value2"
    },
    "responseStatusCode": -1,               <--- can be set by microservices and pipeline steps
    "shouldTerminateRequest": true|false    <--- can be set by microservices and pipeline steps
}
```

### Pipelines

Every http(s) request that comes into the http-nats-proxy can go through a series of steps before being delivered to the
final destination (your microservice). Those steps are defined in a yaml based configuration file and the location of
the file is passed to the https-nats-proxy via the `HTTP_NATS_PROXY_REQUEST_PIPELINE_CONFIG_FILE` environment variable.
Each step defined in the pipeline configuration file contains the following properties:

**subject:** The NATS subject name where the request will be delivered for this particular step in the pipeline. In
order to notify the http-nats-proxy to send the message to your microservice use `*` for the subject name.

**pattern:** Either `publish` or `request`, this tells the http-nats-proxy whether it should do a fire and forget
message exchange pattern or if it should use the request/reply pattern. If you are not going to change or cancel the
http request then you should use `publish`. If your pipeline step will potentially modify the request or cancel the
request you should use `request`

**direction:** Either `incoming, outgoing` or `both`. This tells the http-nats-proxy when to call your pipeline step.
`incoming` is when the message is inbound, `outgoing` is after the message has reached the end of the pipeline and the
response is being returned. `both` will call you pipeline step twice, once when the request is inbound and a second time
when the response is outbound.

**order:** A numeric value that is the order that your step should be called in relation to other steps in the pipeline

### Observers

There are times when you want to get a copy of the request/response message after it has completed running through all
of the pipeline steps. Examples of this would be when you wanted to log the request/response or capture metrics about
how long each request took to process, etc. In these cases, the metadata about the request is not available during the
execution of the pipeline steps. For these scenarios you can use Observers. Observers are notified via a NATS publish
message after all of the pipeline steps have executed and metadata has been stored for the request/response pair. It is
a "copy" of the final state of the http request.

#### Example pipeline-config.yaml file

```
steps:
  - subject: trace.header
    pattern: request
    direction: incoming
    order: 1

  - subject: authentication
    pattern: request
    direction: incoming
    order: 2

  - subject: '*'
    pattern: request
    direction: incoming
    order: 3
observers:
  - subject: 'pipeline.logging'
  - subject: 'pipeline.metrics'
```

## Configuration

All configuration of the http-nats-proxy is done via environment variables.

| Environment Variable                         | Default Value                   | Description                                                                                                                                                |
|----------------------------------------------|---------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| HTTP_NATS_PROXY_HOST_PORT                    | 5000                            | The port that the http-nats-proxy will listen for incoming http requests on                                                                                |
| HTTP_NATS_PROXY_NAT_URL                      | nats://localhost:4222           | The NATS url where the http-nats-proxy will send the NATS message to                                                                                       |
| HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS         | 10                              | The number of seconds that the http-nats-proxy will wait for a response from the microservice backend before it returns a Timeout Error to the http client |
| HTTP_NATS_PROXY_HEAD_STATUS_CODE             | 200                             | The http status code that will be used for a successful HEAD request                                                                                       |
| HTTP_NATS_PROXY_PUT_STATUS_CODE              | 201                             | The http status code that will be used for a successful PUT request                                                                                        |
| HTTP_NATS_PROXY_GET_STATUS_CODE              | 200                             | The http status code that will be used for a successful GET request                                                                                        |
| HTTP_NATS_PROXY_PATCH_STATUS_CODE            | 201                             | The http status code that will be used for a successful PATCH request                                                                                      |
| HTTP_NATS_PROXY_POST_STATUS_CODE             | 201                             | The http status code that will be used for a successful POST request                                                                                       |
| HTTP_NATS_PROXY_DELETE_STATUS_CODE           | 204                             | The http status code that will be used for a successful DELETE request                                                                                     |
| HTTP_NATS_PROXY_CONTENT_TYPE                 | application/json; charset=utf-8 | The http response Content-Type header value. This should be set to whatever messaging format your microservice api supports (xml, json, etc)               |
| HTTP_NATS_PROXY_REQUEST_PIPELINE_CONFIG_FILE |                                 | The full file path and name of the configuration file that specifies your request pipeline                                                                 |

## Running the Demo

In order to see the http-nats-proxy in action along with a test microservice, logging and metrics you can run the docker
compose file in your environment.

```
cd src
docker-compose up
```

This will run the NATS server in a container.

Next start the project from Visual Studio and make sure that each project in the solution is set to run on startup (
multi-project start-up configuration). The http-nats-proxy will listen for http requests on port 5000 of your host
machine. You can then send http requests to the proxy and have them processed by the test microservice. The test
microservice that comes with this repo will respond to the following http routes:

```
GET http://localhost:5000/healthcheck
GET http://localhost:5000/customers?id=<any integer>
```

Additionally, if configured for metrics, logging and tracing you will see the output of those microservices as well in
your terminal. This is for demo purposes only. There is a 1/2 second delay in the logging and metrics microservices just
to simulate work.

## Creating your own docker image

The http-nats-proxy has a `Dockerfile` where you can build your own docker images by running:

```
cd src
docker build -t http-nats-proxy .

```

## Responsibilities of your microservices

In order to control what gets returned from the http-nats-proxy, your microservice has to set the `response` property of
the NATS message that you receive when you subscribe to a NATS subject. You should return the *entire* NATS message. You
may optionally set the `responseStatusCode` and the `errorMessage` properties if an error occurs while you are
processing the message. The nats-http-proxy will honor the `responseStatusCode` if it is set and will also format and
return an error response if the `errorMessage` property has been set.