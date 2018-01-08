# http-nats-proxy
A proxy service that translates http requests to nats messages

## Create a docker image
```
cd src
docker build -t http-nat-proxy .
```

## Environment Variables
HTTP_NATS_PROXY_HOST_PORT

HTTP_NATS_PROXY_NAT_URL

HTTP_NATS_PROXY_WAIT_TIMEOUT_SECONDS
