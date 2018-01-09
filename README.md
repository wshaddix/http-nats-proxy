# http-nats-proxy
A proxy service that translates http requests to nats messages.

## Purpose
This proxy service is designed to be ran in a docker container and will intercept all of your http based api calls (REST endpoints). It will translate the http route to a [NATS](https://nats.io) message and will send the message as a request into the NATS messaging system. Once a reply has been returned it is written back to the http response. The primary use case for this service is when you have an ecosystem of microservices that communicate via NATS but you want http REST api clients to be able to communicate to those services as a typical http REST api client.

## Usage
As a developer, you can test things out by running:

```
cd src
docker-compose up
```

This will run the http-nats-proxy in a container alongside a NATS server and a test microservice. The http-nats-proxy will listen for http requests on port 5000 of your host machine. You can then send http requests to the proxy and have them processed by your microservices. The test microservice that comes with this repo will respond to the following http routes:

```
GET http://localhost:5000/test/v1/customer
PUT http://localhost:5000/test/v1/customer
POST http://localhost:5000/test/v1/customer
```


## Creating your own docker image
The http-nats-proxy has a `Dockerfile` where you can build your own docker images by running:

```
cd src
docker build -t http-nats-proxy .
```

## Environment Variables
The following environment variables can be used to alter the url, port and timeout of the http-nats-proxy:

**HTTP_NATS_PROXY_HOST_PORT**

Controls which port the http-nats-proxy listens for http requests. The default is 5000

**HTTP_NATS_PROXY_NAT_URL**

The URL to the NATS server. The default is nats://localhost:4222

**HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS**

How many seconds to wait on a reply from a microservice before throwing a Timeout exception. The default is 10

## Running in docker
If you want to run the http-nats-proxy in your docker environment and access it on port 5000 you can run:

```
docker run --rm -it -p 5000:5000 docker wshaddix/http-nats-proxy
```

## Routing
When an http request is received the http-nats-proxy will take the request path and replace every forward slash ('/') with a period ('.'). It will then prefix that with the http verb that was used (get, put, post, delete, head, etc.). Finally it will convert the entire string to lowercase. This becomes your NATS subject where the message will be sent.

For example, the http request of `GET http://localhost:5000/test/v1/customer?id=1234&show-history=true` will result in a NATS message subject of `get.test.v1.customer`

## Data Format
The NATS message that goes into the messaging system will be a json serialized object that has the following structure:

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
	"body": ''
}
```

Each of the http request headers, cookies and query parameters will be represented as a key/value pair. The body will be represented as a string.