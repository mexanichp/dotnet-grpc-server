# Dotnet gRPC server
.NET 5 gRPC project. Server-side for mobile app clients.

Copyright 2021, Mykhailo Konovalov, All rights reserved.

## Architecture guideline
* .NET Core and gRPC for server-side development
    * Protobuf as a model serializer
    * gRPC unary message support
    * OAuth2.0 with 3d party IDP (Firebase)
    * MediatR as a handler resolver
    * Mapster as a mapping framework
    * Dynamic DB schema design
* MongoDB as a database
* Serilog with Seq as a logging framework
* Nginx as a reverse proxy
* docker-compose as a development containerization orchestrator

## Project structure
* `protos` - protobuf message definitions
* `server` - .NET 5 gRPC solution
    * `/HealthyPlant.Data` - DB layer
    * `/HealthyPlant.Domain` - Domain logic layer
    * `/HealthyPlant.Grpc` - gRPC facade layer
        * `/GrpcServices` - gRPC service definitions (aka Controllers in Web API)
        * `/Commands` - command handlers logic
        * `/Jobs` - scheduled job definitions
        * `/Infrastructure` - logging and gRPC extensions, middlewares, interceptors
    * `/HealthyPlant.Tests`- integration tests
    * `/Infrastructure` - common infrastructure code