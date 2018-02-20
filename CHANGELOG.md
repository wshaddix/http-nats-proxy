### Changes

**0.9.1**

* now honoring the `NatsMessage.responseStatusCode` if it has been set by the microservice handling the request.

* if the `NatsMessage.errorMessage` property is set then the http-nats-proxy will return a status code 500 with a formatted error message to the api client.

* now returning the `NatsMessage.response` as the http response.

**1.0.0**

* added pipeline feature and refactored the solution to have working examples of logging, metrics, authentiation and trace header injection.

**1.0.1**

* added logging to the request handler so that it's obvious when traffic is coming into the http-nats-proxy