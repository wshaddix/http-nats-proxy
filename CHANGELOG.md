# Changes

## 0.9.1

* now honoring the `NatsMessage.responseStatusCode` if it has been set by the microservice handling the request.

* if the `NatsMessage.errorMessage` property is set then the http-nats-proxy will return a status code 500 with a formatted error message to the api client.

* now returning the `NatsMessage.response` as the http response.

## 1.0.0

* added pipeline feature and refactored the solution to have working examples of logging, metrics, authentiation and trace header injection.

## 1.0.1

* added logging to the request handler so that it's obvious when traffic is coming into the http-nats-proxy

## 1.0.2

* now checking the state of the NATS connection to ensure it's in a `CONNECTED` state before trying to send messages.
* reduced the `PingInterval` of the NATS connection to help with reconnections when the NATS server becomes unavailable
* now logging when NATS connection events happen

## 1.0.3

* changing the format of the message that the proxy sends to microservices and the pipeline steps to make them more intuitive. Specifically changed `Cookies, ExtendedProperties, QueryParams, RequestHeaders` and `ResponseHeaders` from name/value collections to `Dictionary<string, object>` so they serialize into a more intuitive json string.

## 1.0.4

* changed the call timings from a tuple to a custom class because tuples do not serialize to json with readable property names (see https://github.com/JamesNK/Newtonsoft.Json/issues/1230)
* renamed property `Body` to `RequestBody` on the `NatsMessage`
* renamed property `Response` to `ResponseBody` on the `NatsMessage`
* introduced the concept of Observers to the request pipeline (see README for details)

## 1.0.5

* enabling CORS

## 1.0.6

* bugfix - when CORS was enabled in release 1.0.5 I didn't fully enable it b/c i didn't include the .AllowCredentials() method on the CORS policy builder