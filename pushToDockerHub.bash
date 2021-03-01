#!/bin/bash
services=(ApiGateway CatalogService NotificationService OrderService UserService)

for service in ${services[*]}; do
    tag="ankur198/nagp-${service}"
    path="Src/${service}"
    echo $service "${tag,,}" $path
    docker build -t "${tag,,}" $path
    docker push "${tag,,}"
done