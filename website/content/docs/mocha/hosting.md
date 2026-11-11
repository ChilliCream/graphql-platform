---
title: "Hosting"
description: "Integrate Mocha with ASP.NET Core using the Mocha.Hosting package for health checks."
---

The `Mocha.Hosting` package provides ASP.NET Core integrations for the message bus: health checks that verify end-to-end connectivity through serialization, transport, routing, and handler execution.

```shell
dotnet add package Mocha.Hosting
```

# Health checks

Mocha integrates with the [ASP.NET Core health checks](https://learn.microsoft.com/aspnet/core/host-and-monitor/health-checks) system. The health check sends a `HealthRequest` message through the bus and waits for a `HealthResponse`. This verifies the full pipeline - serialization, transport, routing, and handler execution - not just that the broker is reachable.

## Register the health check handler

On the bus builder, call `.AddHealthCheck()` to register the built-in `HealthRequestHandler`. This handler responds to `HealthRequest` messages with an `"OK"` response.

```csharp
using Mocha;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddHealthCheck() // Registers the HealthRequestHandler
    .AddMyApp()       // source-generated handler registration
    .AddRabbitMQ();
```

## Add the health check to ASP.NET Core

Use the `AddMessageBus()` extension on `IHealthChecksBuilder` to register a health check that sends a request through the bus and verifies the response:

```csharp
builder.Services
    .AddHealthChecks()
    .AddMessageBus(); // Sends HealthRequest via RequestAsync and checks the reply
```

Then map the health check endpoint:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
```

A `GET /health` request includes the message bus status. The check fails if any local transport is unhealthy or the bus does not answer the health request within the timeout. The `Data` of the health check entry lists each unhealthy transport by name with its reason. RabbitMQ transport health includes broker connectivity, consumer attachment, and capacity occupied by canceled handlers that have not exited. Normally busy handlers do not make the transport unhealthy.

## Configure the health check

`AddMessageBus()` takes optional parameters:

| Parameter       | Default        | Description                                                               |
| --------------- | -------------- | ------------------------------------------------------------------------- |
| `endpoint`      | `null`         | Destination of the health request. `null` uses the bus's default routing. |
| `name`          | `"MessageBus"` | Name of the health check entry.                                           |
| `failureStatus` | `Unhealthy`    | Status reported when the check fails.                                     |
| `tags`          | `["ready"]`    | Tags of the health check.                                                 |
| `timeout`       | 10 seconds     | Time within which the bus must answer the health request.                 |

```csharp
builder.Services
    .AddHealthChecks()
    .AddMessageBus(
        name: "orders-bus",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "messaging"],
        timeout: TimeSpan.FromSeconds(5));
```

The health request expires after the timeout. A request that was not handled by then is dropped when it is received.

## Target a specific endpoint

By default the health check uses the bus's default routing to deliver the `HealthRequest`. To target a specific endpoint (useful when you have multiple transports or want to verify a particular service), pass a URI:

```csharp
builder.Services
    .AddHealthChecks()
    .AddMessageBus(new Uri("queue://my-service-health"));
```

The endpoint URI selects the destination of the health request. Every local transport must still be healthy.

## Readiness and liveness

The message bus check has the `"ready"` tag unless you pass other `tags`. Use separate, explicitly filtered endpoints for readiness and liveness:

```csharp
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new()
{
    Predicate = check => check.Tags.Contains("live")
});
```

The liveness endpoint runs only checks registered with the `"live"` tag. With none registered, it returns healthy while the application can serve the request. Broker outages and consumer recovery affect readiness without failing liveness. An unfiltered health endpoint includes the bus check and must not be used as a process liveness probe.

# Next steps

- [Observability](./observability.md) - Add OpenTelemetry tracing and metrics to the bus.
- [Reliability](./reliability.md) - Configure outbox, inbox, and circuit breakers.
- [Transports](./transports/index.md) - Configure RabbitMQ, InMemory, and multi-transport setups.
