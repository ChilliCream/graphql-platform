---
title: "Integrating Fusion with Aspire"
---

# Introduction

<Video videoId="AHitpPCeM00" />

Integrating Fusion with [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) enhances your development workflow by automating the composition of your gateway and subgraphs during the build process.
Aspire acts as an internal orchestrator, streamlining the setup and management of distributed systems and microservices.
By configuring your AppHost to compose all referenced source systems on build, any changes made to downstream services are immediately reflected, ensuring you always have the latest composed version running in development.

# Overview of Aspire

[Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) is an internal orchestrator designed to streamline your development experience when working with distributed systems and microservices.

It offers:

- [**Service Defaults**](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults): Extension methods that add essential configurations to your services, such as telemetry, health endpoints, and service discovery.
- [**AppHost**](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview?tabs=docker): An orchestrator that defines dependencies between your applications and automates the resolution and startup of these dependencies.

By integrating Aspire with Fusion, you can:

- Automatically compose your Fusion gateway with all referenced subgraphs on build.
- Simplify the configuration and management of your federated GraphQL services.
- Enhance your debugging experience with built-in telemetry and logging.

# Setting Up the AppHost with Fusion and Aspire

To integrate Fusion with Aspire, you need to configure your AppHost to include your gateway and subgraphs.
The AppHost serves as the central orchestrator, managing dependencies and automating the composition process during the build.

Below is an example of how to set up your AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var ordering = builder.AddProject<Projects.quick_start_Ordering>("ordering");
var products = builder.AddProject<Projects.quick_start_Products>("products");

builder
    .AddFusionGateway<Projects.quick_start_Gateway>("gateway")
    .WithSubgraph(ordering)
    .WithSubgraph(products);

// Important: Use 'Compose' before 'Run' to enable build-time composition
builder.Build().Compose().Run();
```

In this configuration:

- **Create the Builder**: Initialize a new distributed application builder with `DistributedApplication.CreateBuilder(args)`.
- **Add Subgraph Projects**: Include your subgraph projects (e.g., `ordering` and `products`) using `builder.AddProject<TProject>("name")`. These represent your downstream services or subgraphs.
- **Configure the Fusion Gateway**: Add your Fusion gateway project with `builder.AddFusionGateway<TProject>("name")` and associate the subgraphs using `.WithSubgraph(subgraph)`.
- **Compose and Run**: Use `builder.Build().Compose().Run();` instead of the usual `builder.Build().Run();`. The `.Compose()` method triggers the composition of your federated schema during the build process.

By including `.Compose()`, Aspire automatically composes the gateway every time you build the application. This ensures that any changes made to your subgraphs are incorporated into the Fusion gateway without manual intervention, providing an up-to-date composed schema whenever you run your application.

# Options

You can further customize the composition process by using the `WithOptions` to configure specific settings for your gateway.

```csharp
services
    .AddGraphQLServer()
    .AddFusion()
    .WithOptions(new FusionCompositionOptions
    {
        // equivalent to `--enable-nodes` CLI option
        EnableGlobalObjectIdentification = true
    });
```

# Example Repository

A practical example demonstrating this integration is available in the [HotChocolate Examples Repository](https://github.com/ChilliCream/hotchocolate-examples/tree/master/fusion/aspire) under the `fusion/aspire` directory. This repository illustrates:

- Setting up the AppHost with multiple subgraphs and a Fusion gateway.
- Configuring service defaults and endpoints.
- Running the application to see the composed schema in action.

By exploring this example, you can gain a deeper understanding of how to implement the integration in your own projects.
