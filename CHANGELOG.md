### Changes

**0.9.1**

* now honoring the `NatsMessage.responseStatusCode` if it has been set by the microservice handling the request.

* if the `NatsMessage.errorMessage` property is set then the http-nats-proxy will return a status code 500 with a formatted error message to the api client.

* now returning the `NatsMessage.response` as the http response.