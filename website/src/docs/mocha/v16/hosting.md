---
title: "Hosting"
description: "Integrate Mocha with ASP.NET Core using the Mocha.Hosting package for health checks."
---

# Hosting

The `Mocha.Hosting` package provides ASP.NET Core integrations for the message bus: health checks that verify end-to-end connectivity through serialization, transport, routing, and handler execution.

```bash
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

A `GET /health` request will now include the message bus status. If the bus cannot process and reply to the health request within the timeout, the check reports `Unhealthy`.

## Target a specific endpoint

By default the health check uses the bus's default routing to deliver the `HealthRequest`. To target a specific endpoint (useful when you have multiple transports or want to verify a particular service), pass a URI:

```csharp
builder.Services
    .AddHealthChecks()
    .AddMessageBus(new Uri("queue://my-service-health"));
```

The health check is registered with the `"ready"` and `"live"` tags, so you can use tag-based filtering to separate readiness from liveness probes:

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

# Next steps

- [Observability](/docs/mocha/v16/observability) - Add OpenTelemetry tracing and metrics to the bus.
- [Reliability](/docs/mocha/v16/reliability) - Configure outbox, inbox, and circuit breakers.
- [Transports](/docs/mocha/v16/transports) - Configure RabbitMQ, InMemory, and multi-transport setups.
