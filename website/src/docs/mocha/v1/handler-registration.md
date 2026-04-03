---
title: "Handler Registration"
description: "How Mocha discovers message bus handlers at compile time using a Roslyn source generator, and how to customize module naming, mix auto and manual registration, and resolve analyzer diagnostics."
---

```csharp
builder.Services
    .AddMessageBus()
    .AddOrderService(); // source-generated from your assembly name
```

# Handler registration

That registers the message bus, discovers your handlers and sagas at compile time, and wires up the registration. `.AddOrderService()` is a source-generated extension method - it knows your handler types at compile time and produces direct registration calls with no reflection.

# How the source generator works

At build time, the `Mocha.Analyzers` package runs a [Roslyn incremental source generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) that scans your assembly for classes implementing message bus handler interfaces. For each handler it finds, it emits a registration call in a generated extension method on `IMessageBusHostBuilder`.

The generator discovers these types:

| Interface                          | Pattern                |
| ---------------------------------- | ---------------------- |
| `IEventHandler<T>`                 | Pub/sub events         |
| `IEventRequestHandler<TReq, TRes>` | Request/reply          |
| `IEventRequestHandler<TReq>`       | Send (fire-and-forget) |
| `IBatchEventHandler<T>`            | Batch processing       |
| `IConsumer<T>`                     | Low-level consumer     |
| `Saga<TState>`                     | Saga orchestration     |

If you have used the [Mediator source generator](/docs/mocha/v1/mediator), this works the same way. The mediator generates `Add{ModuleName}()` on `IMediatorHostBuilder`; the message bus generates `Add{ModuleName}()` on `IMessageBusHostBuilder`.

> **Recommendation:** Always use the source generator for handler registration. The generated code uses optimized, reflection-free registration paths. The source-generated output is designed for long-term stability across versions. Manual registration methods are available for edge cases but their internal behavior may change between releases.

# Module naming

The source generator names the extension method based on your assembly:

1. If you apply `[assembly: MessagingModule("OrderService")]`, the method is `AddOrderService()`
2. Otherwise, it uses the last segment of the assembly name: `MyCompany.OrderService.Api` produces `AddApi()`

To set an explicit module name, add the attribute to any file in your project:

```csharp
using Mocha;

[assembly: MessagingModule("OrderService")]
```

This generates:

```csharp
builder.Services
    .AddMessageBus()
    .AddOrderService() // from [assembly: MessagingModule("OrderService")]
    .AddRabbitMQ();
```

> **Convention:** Use a short, meaningful name that identifies the service or bounded context - `OrderService`, `Billing`, `Inventory`. This name appears in the generated code and in your `Program.cs`, so keep it readable.

# What the generator produces

For a project named `OrderService` with an event handler, a request handler, and a saga, the source generator produces an extension method `AddOrderService()` on `IMessageBusHostBuilder`. This method registers all discovered handlers and sagas with optimized, reflection-free factory delegates.

Handlers are grouped by kind and ordered alphabetically within each group. The registration order is: batch handlers, consumers, request handlers, event handlers, sagas.

> **Note:** The generated code is an implementation detail and may change between versions. Do not depend on the shape of the generated output.

# Manual handler registration

When you need to register handlers outside the source generator's reach - from a plugin assembly, a dynamically loaded module, or in integration tests - use the explicit registration methods:

```csharp
builder.Services
    .AddMessageBus()
    .AddOrderService()                                    // source-generated handlers
    .AddEventHandler<ExternalNotificationHandler>()       // from another assembly
    .AddRequestHandler<PluginPaymentHandler>()            // from a plugin
    .AddRabbitMQ();
```

You can mix source-generated and manual registration freely. If both the source generator and manual code register the same handler type, the configurations are composed - the source generator sets up the base registration and your manual call layers additional configuration (such as consumer middleware) on top.

> **Prefer the source generator.** Manual registration methods use runtime reflection to create handler consumers. The source generator produces direct, reflection-free factory calls. We guarantee backwards compatibility for the source-generated registration path; the manual registration API is stable at the surface level but its internal behavior may evolve.

# Troubleshooting

## The source-generated method does not appear

If IntelliSense does not show `Add{ModuleName}()`:

- Confirm the `Mocha.Analyzers` package is referenced with `OutputItemType="Analyzer"` in your `.csproj`
- Rebuild the project - source generators run during compilation
- Check the build output for [analyzer diagnostics](/docs/mocha/v1/diagnostics) prefixed with `MO`
- Verify you have at least one concrete handler class in the assembly

## Handler is not being called

If the source-generated method is available but a specific handler does not run:

- Check for [**MO0013**](/docs/mocha/v1/diagnostics#mo0013) (abstract handler) - only concrete classes are registered
- Check for [**MO0012**](/docs/mocha/v1/diagnostics#mo0012) (open generic) - close the generic type
- Verify the handler implements the correct interface for the messaging pattern you are using
- Ensure the handler is in the same project that references `Mocha.Analyzers`

## Priority when a handler implements multiple interfaces

If a class implements more than one messaging interface (e.g., both `IBatchEventHandler<T>` and `IEventHandler<T>`), the source generator registers it using the highest-priority interface only:

`IBatchEventHandler` > `IConsumer` > `IEventRequestHandler<T,R>` > `IEventRequestHandler<T>` > `IEventHandler`

# Next steps

- [Handlers and Consumers](/docs/mocha/v1/handlers-and-consumers) - handler interfaces, DI scoping, and exception behavior
- [Routing and Endpoints](/docs/mocha/v1/routing-and-endpoints) - how the bus routes messages to registered handlers
- [Sagas](/docs/mocha/v1/sagas) - saga state machines and long-running workflows
- [Mediator](/docs/mocha/v1/mediator) - the mediator uses the same source generation approach for in-process CQRS
