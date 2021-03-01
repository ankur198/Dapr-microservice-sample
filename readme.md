# How to Run?

To use images from docker hub: `docker-compose up`

To build images `docker-compose -f docker-compose.yml -f docker-compose.dev.yml --build`

## Windows: Extra requirement

Windows requires **file sharing** to be turned on the project location.
This is required because dapr sidecar need configuration to be loaded at runtime from volume mount
