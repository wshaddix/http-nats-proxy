# http-nats-proxy
A proxy service that converts http requests to [NATS](https://nats.io) messages.

## Purpose
This proxy service is designed to be ran in a docker container and will translate all of your http based api calls (REST endpoints). It will translate the http route to a [NATS](https://nats.io) message and will send the message as a request into the NATS messaging system. Once a reply has been returned it is written back to the http response. The primary use case for this service is when you have an ecosystem of microservices that communicate via NATS but you want http REST api clients to be able to communicate to those services as a typical http REST api client.

## Features

### Routing

When an http request is received the http-nats-proxy will take the request path and replace every forward slash ('/') with a period ('.'). It will then prefix that with the http verb that was used (get, put, post, delete, head, etc.). Finally it will convert the entire string to lowercase. This becomes your NATS subject where the message will be sent.
For example, the http request of `GET http://localhost:5000/test/v1/customer?id=1234&show-history=true` will result in a NATS message subject of `get.test.v1.customer`

### HTTP Request Format Translation

When an http request is received, the http-nats-proxy will extract the headers, cookies, query string parameters and the body of the request and create a NATS message. The NATS message that goes into the messaging system will be a json serialized object that has the following structure:

```
{
	"headers": [
		{"key": '', "value": ''}.
		{"key": '', "value": ''}
	],
	"cookies": [
		{"key": '', "value": ''}.
		{"key": '', "value": ''}
	],
	"queryParams": [
		{"key": '', "value": ''}.
		{"key": '', "value": ''}
	],
	"body": '',
	"responseStatusCode": -1,
	"response": '',
	"errorMessage", ''
}
```

Each of the http request headers, cookies and query parameters will be represented as a key/value pair. The body will be represented as a string.

### Metrics

The http-nats-proxy can be configured to collect metrics. The information collected is the name of the subject, the utc date/time of the api call as well as how long the system took to provide a response. From that information you can determine response times and call volume per api request. See the Configuration section for details on how to enable metrics.

### Logging

The http-nats-proxy can be configured to log http request/response pairs. In addition to the request/response it will also include metadata such as the subject, response code, execution time (if metrics are enabled), etc. See the Configuration section for details on how to enable logging.

### Tracing

The http-nats-proxy can be configured to inject a trace header into the http request before sending it to the NATS messaging system. This can be used to track which microservices worked on an api call as well as to correlate logs with metrics (both will contain the trace id). See the Configuration section for details on how to enable tracing.

## Configuration
All configuration of the http-nats-proxy is done via environment variables.

| Environment Variable                 | Default Value                   | Description                              |
|--------------------------------------|---------------------------------|------------------------------------------|
| HTTP_NATS_PROXY_HOST_PORT            | 5000                            | The port that the http-nats-proxy will listen for incoming http requests on |
| HTTP_NATS_PROXY_NAT_URL              | nats://localhost:4222           | The NATS url where the http-nats-proxy will send the NATS message to |
| HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS | 10                              | The number of seconds that the http-nats-proxy will wait for a response from the microservice backend before it returns a Timeout Error to the http client |
| HTTP_NATS_PROXY_HEAD_STATUS_CODE     | 200                             | The http status code that will be used for a successful HEAD request |
| HTTP_NATS_PROXY_PUT_STATUS_CODE      | 201                             | The http status code that will be used for a successful PUT request |
| HTTP_NATS_PROXY_GET_STATUS_CODE      | 200                             | The http status code that will be used for a successful GET request |
| HTTP_NATS_PROXY_PATCH_STATUS_CODE    | 201                             | The http status code that will be used for a successful PATCH request |
| HTTP_NATS_PROXY_POST_STATUS_CODE     | 201                             | The http status code that will be used for a successful POST request |
| HTTP_NATS_PROXY_DELETE_STATUS_CODE   | 204                             | The http status code that will be used for a successful DELETE request |
| HTTP_NATS_PROXY_CONTENT_TYPE         | application/json; charset=utf-8 | The http response Content-Type header value. This should be set to whatever messaging format your microservice api supports (xml, json, etc) |
| HTTP_NATS_PROXY_METRICS_SUBJECT      |                                 | If set, this is the NATS subject that metrics will be published to |
| HTTP_NATS_PROXY_LOGS_SUBJECT         |                                 | If set, this is the NATS subject that logs will be published to |
| HTTP_NATS_PROXY_TRACE_HEADER         |                                 | If set, this is the http request header name that will be injected into each http request with a globally unique value for a trace id. If the http request header already exists then it will not be overwritten |



## Running the Demo
In order to see the http-nats-proxy in action along with a test microservice, logging and metrics you can run the docker compose file in your environment.

```
cd src
docker-compose up
```

This will run the http-nats-proxy in a container alongside a NATS server and a test microservice. The http-nats-proxy will listen for http requests on port 5000 of your host machine. You can then send http requests to the proxy and have them processed by the test microservice. The test microservice that comes with this repo will respond to the following http routes:

```
GET http://localhost:5000/test/v1/customer
PUT http://localhost:5000/test/v1/customer  <-- will override the response status code to be 200 (example of response status code overriding)
POST http://localhost:5000/test/v1/customer
DELETE http://localhost:5000/test/v1/customer <-- will return an error stating that the customer cannot be deleted (example of returning an error from your microservice)
```

Additionally, if configured for metrics, logging and tracing you will see the output of those microservices as well in your terminal. This is for demo purposes only. There is a 1/2 second delay in the logging and metrics microservices just to simulate work.

## Creating your own docker image
The http-nats-proxy has a `Dockerfile` where you can build your own docker images by running:

```
cd src
docker build -t http-nats-proxy .

```

## Debugging your microservices
It can be useful to have NATS and the http-nats-proxy running while you are coding and debugging your microservices. In order to assist with this scenario there is a docker compose file that will run NATS and the http-nats-proxy where you can test out your microservice easily. The steps to do this are:

1. From the `src` directory run
```
docker-compose -f docker-compose-nats-only.yml up
```

2. Code your microservice and have it connect to nats at `nats://localhost:4222`
3. Once your microservice is ready to test, send in http requests through CURL, Postman or whatever means you want to the http-nats-proxy at `http://localhost:5000`

## Responsibilities of your microservices

In order to control what gets returned from the http-nats-proxy, your microservice has to set the `response` property of the NATS message that you receive when you subscribe to a NATS subject. You should return the *entire* NATS message. You may optionally set the `responseStatusCode` and the `errorMessage` properties if an error occurs while you are processing the message. The nats-http-proxy will honor the `responseStatusCode` if it is set and will also format and return an error response if the `errorMessage` property has been set.