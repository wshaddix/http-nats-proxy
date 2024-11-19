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

## 1.1.0

* extracted some types and helper classes into Proxy.Shared and made into a nuget package for other c# based microservices to leverage
* refactored the example handlers (handlers and observers) to use the Proxy.Shared library
* updated the docker image to use dotnet core 2.1 alpine
* updated logging to use serilog instead of console.writeline
* updated referenced nuget packages to their latest versions
* refactored the request handler to be less bloated and more focused
* updated nats to version 1.2.0
* now compiling against .net core 2.1.300
* added a .TryGetParam() method to the MicroserviceMessage to make it easier to get at headers, cookies and query string params

## 1.1.1

* fixing issue where response type was not getting set 

## 1.1.2

* code cleanup
* updated .net core version to 2.2.301
* updated docker images to mcr.microsoft.com/dotnet/core/sdk:2.2.301-alpine3.9 and mcr.microsoft.com/dotnet/core/runtime:2.2.6-alpine3.9
* updated NATS server version to 1.4.1
* updated projects target framework to .Net Core 2.2
* merged in PR to fix parsing of extended properties during pipeline execution (thanks to https://github.com/timsmid)
* updated all nuget dependencies

## 1.2.0

* Updated to .Net 9
* Migrated to centralized package management
* Updated log messages to follow best practices (no trailing period)
* Fixed typo in code comment
* Added additional error handling and null checks
* Updated any outdated or vulnerable NuGet packages
* Updated Dockerfile to latest versions and addressed scout vulnerabilities