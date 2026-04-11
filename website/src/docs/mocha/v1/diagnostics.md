---
title: "Diagnostics"
description: "Reference for all compile-time diagnostics emitted by the Mocha source generator, including causes, examples, and fixes for each warning and error."
---

# Diagnostics

Mocha uses a Roslyn source generator to validate your message handlers, consumers, and sagas at compile time. When the generator detects a problem - a missing handler, a duplicate registration, an invalid type - it emits a diagnostic that appears as a compiler warning or error in your IDE and build output. You can fix these issues before your code ever runs.

# Quick reference

| Code              | Description                                              | Severity |
| ----------------- | -------------------------------------------------------- | -------- |
| [MO0001](#mo0001) | Missing handler for message type                         | Warning  |
| [MO0002](#mo0002) | Duplicate handler for message type                       | Error    |
| [MO0003](#mo0003) | Handler is abstract                                      | Warning  |
| [MO0004](#mo0004) | Open generic message type cannot be dispatched           | Info     |
| [MO0005](#mo0005) | Handler implements multiple mediator handler interfaces  | Error    |
| [MO0011](#mo0011) | Duplicate handler for request type                       | Error    |
| [MO0012](#mo0012) | Open generic messaging handler cannot be auto-registered | Info     |
| [MO0013](#mo0013) | Messaging handler is abstract                            | Warning  |
| [MO0014](#mo0014) | Saga must have a public parameterless constructor        | Error    |
| [MO0015](#mo0015) | Missing JsonSerializerContext for AOT                    | Error    |
| [MO0016](#mo0016) | Missing JsonSerializable attribute                       | Error    |
| [MO0018](#mo0018) | Type not in JsonSerializerContext                        | Warning  |
| [MO0020](#mo0020) | Command/query sent but no handler found                  | Warning  |

# Mediator diagnostics

These diagnostics apply to the in-process [mediator](/docs/mocha/v1/mediator) - commands, queries, and notifications dispatched within a single process.

## MO0001

**Missing handler for message type**

|              |                                                |
| ------------ | ---------------------------------------------- |
| **Severity** | Warning                                        |
| **Message**  | `Message type '{0}' has no registered handler` |

### Cause

A command or query type is declared but no corresponding handler implementation exists. The mediator requires exactly one handler for each command and query type. This diagnostic does not apply to notifications, which can have zero handlers.

### Example

```csharp
using Mocha.Mediator;

// Command with no handler - triggers MO0001
public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;
```

### Fix

Implement a handler for the message type.

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;

public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(
        PlaceOrder command,
        CancellationToken cancellationToken)
    {
        // process the order
        return ValueTask.CompletedTask;
    }
}
```

## MO0002

**Duplicate handler for message type**

|              |                                                 |
| ------------ | ----------------------------------------------- |
| **Severity** | Error                                           |
| **Message**  | `Message type '{0}' has multiple handlers: {1}` |

### Cause

A command or query type has more than one handler implementation. Commands and queries require exactly one handler - the mediator cannot decide which one to call. This diagnostic does not apply to notifications, which support multiple handlers by design.

### Example

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;

// Two handlers for the same command - triggers MO0002
public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(PlaceOrder command, CancellationToken ct)
        => ValueTask.CompletedTask;
}

public class DuplicateOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(PlaceOrder command, CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

### Fix

Remove all but one handler. If you need multiple side effects for the same action, consider publishing a notification from the single handler and reacting to it with separate notification handlers.

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;

public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(PlaceOrder command, CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

## MO0003

**Handler is abstract**

|              |                                                        |
| ------------ | ------------------------------------------------------ |
| **Severity** | Warning                                                |
| **Message**  | `Handler '{0}' is abstract and will not be registered` |

### Cause

A class implements a [handler](/docs/mocha/v1/handlers-and-consumers) interface (`ICommandHandler`, `IQueryHandler`, or `INotificationHandler`) but is declared `abstract`. The source generator skips abstract types because they cannot be instantiated.

### Example

```csharp
using Mocha.Mediator;

public record GetOrderTotal(Guid OrderId) : IQuery<decimal>;

// Abstract handler - triggers MO0003
public abstract class GetOrderTotalHandler : IQueryHandler<GetOrderTotal, decimal>
{
    public abstract ValueTask<decimal> HandleAsync(
        GetOrderTotal query,
        CancellationToken cancellationToken);
}
```

### Fix

Make the handler concrete. If you want shared base logic, move it to a base class that does not implement the handler interface, and have the concrete handler extend it.

```csharp
using Mocha.Mediator;

public record GetOrderTotal(Guid OrderId) : IQuery<decimal>;

public class GetOrderTotalHandler : IQueryHandler<GetOrderTotal, decimal>
{
    public ValueTask<decimal> HandleAsync(
        GetOrderTotal query,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(99.99m);
    }
}
```

## MO0004

**Open generic message type cannot be dispatched**

|              |                                                                             |
| ------------ | --------------------------------------------------------------------------- |
| **Severity** | Info                                                                        |
| **Message**  | `Message type '{0}' is an open generic and cannot be dispatched at runtime` |

### Cause

A command or query type has unbound type parameters. The mediator dispatches concrete types at runtime and cannot resolve an open generic like `MyCommand<T>`.

### Example

```csharp
using Mocha.Mediator;

// Open generic command - triggers MO0004
public record ProcessItem<T>(T Item) : ICommand;
```

### Fix

Use concrete message types instead.

```csharp
using Mocha.Mediator;

public record ProcessOrder(Guid OrderId) : ICommand;
public record ProcessPayment(decimal Amount) : ICommand;
```

## MO0005

**Handler implements multiple mediator handler interfaces**

|              |                                                                       |
| ------------ | --------------------------------------------------------------------- |
| **Severity** | Error                                                                 |
| **Message**  | `Handler '{0}' must implement exactly one mediator handler interface` |

### Cause

A single class implements more than one of `ICommandHandler`, `IQueryHandler`, or `INotificationHandler`. Each handler class must implement exactly one mediator handler interface so the generator can produce unambiguous registrations.

### Example

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId) : ICommand;
public record GetOrder(Guid OrderId) : IQuery<Order>;

// Implements both command and query handler - triggers MO0005
public class OrderHandler
    : ICommandHandler<PlaceOrder>,
      IQueryHandler<GetOrder, Order>
{
    public ValueTask HandleAsync(PlaceOrder command, CancellationToken ct)
        => ValueTask.CompletedTask;

    public ValueTask<Order> HandleAsync(GetOrder query, CancellationToken ct)
        => ValueTask.FromResult(new Order());
}
```

### Fix

Split into separate handler classes, one per interface.

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId) : ICommand;
public record GetOrder(Guid OrderId) : IQuery<Order>;

public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(PlaceOrder command, CancellationToken ct)
        => ValueTask.CompletedTask;
}

public class GetOrderHandler : IQueryHandler<GetOrder, Order>
{
    public ValueTask<Order> HandleAsync(GetOrder query, CancellationToken ct)
        => ValueTask.FromResult(new Order());
}
```

# Messaging diagnostics

These diagnostics apply to the [message bus](/docs/mocha/v1/handlers-and-consumers) - event handlers, request handlers, batch handlers, consumers, and sagas that communicate across service boundaries.

## MO0011

**Duplicate handler for request type**

|              |                                                 |
| ------------ | ----------------------------------------------- |
| **Severity** | Error                                           |
| **Message**  | `Request type '{0}' has multiple handlers: {1}` |

### Cause

A request type (used with `SendAsync` or `RequestAsync`) has more than one [handler](/docs/mocha/v1/handlers-and-consumers) implementation. Request types require exactly one handler - the bus cannot route to multiple targets.

### Example

```csharp
using Mocha;

public record ProcessPayment(decimal Amount);

// Two handlers for the same request type - triggers MO0011
public class PaymentHandlerA : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(
        ProcessPayment request,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}

public class PaymentHandlerB : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(
        ProcessPayment request,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

### Fix

Keep one handler per request type.

```csharp
using Mocha;

public record ProcessPayment(decimal Amount);

public class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(
        ProcessPayment request,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

## MO0012

**Open generic messaging handler cannot be auto-registered**

|              |                                                                  |
| ------------ | ---------------------------------------------------------------- |
| **Severity** | Info                                                             |
| **Message**  | `Handler '{0}' is an open generic and cannot be auto-registered` |

### Cause

A messaging handler (`IEventHandler<T>`, `IEventRequestHandler<T>`, `IBatchEventHandler<T>`, or `IConsumer<T>`) has unbound type parameters. The source generator cannot produce registration code for open generic types.

### Example

```csharp
using Mocha;

// Open generic handler - triggers MO0012
public class GenericEventHandler<T> : IEventHandler<T>
{
    public ValueTask HandleAsync(
        T message,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

### Fix

Make the handler concrete. If you need to handle multiple event types with shared logic, create a concrete handler for each type and extract the shared logic into a base class or shared service.

If you need to register an open generic handler, register it manually through DI instead of relying on auto-registration.

```csharp
using Mocha;

public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

## MO0013

**Messaging handler is abstract**

|              |                                                        |
| ------------ | ------------------------------------------------------ |
| **Severity** | Warning                                                |
| **Message**  | `Handler '{0}' is abstract and will not be registered` |

### Cause

A class implements a messaging [handler](/docs/mocha/v1/handlers-and-consumers) interface but is declared `abstract`. The source generator skips abstract types because they cannot be instantiated.

### Example

```csharp
using Mocha;

public record OrderPlaced(Guid OrderId);

// Abstract handler - triggers MO0013
public abstract class OrderEventHandler : IEventHandler<OrderPlaced>
{
    public abstract ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct);
}
```

### Fix

Make the handler concrete. If you need shared base logic, move it to a base class that does not implement the handler interface.

```csharp
using Mocha;

public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}
```

## MO0014

**Saga must have a public parameterless constructor**

|              |                                                           |
| ------------ | --------------------------------------------------------- |
| **Severity** | Error                                                     |
| **Message**  | `Saga '{0}' must have a public parameterless constructor` |

### Cause

A [`Saga<TState>`](/docs/mocha/v1/sagas) subclass does not have a public parameterless constructor. The saga infrastructure requires this constructor to instantiate the saga type. This is enforced by the `new()` constraint on the `AddSaga<T>` registration method.

### Example

```csharp
using Mocha.Sagas;

public class RefundSagaState : SagaStateBase
{
    public Guid OrderId { get; set; }
}

// Constructor requires a parameter - triggers MO0014
public class RefundSaga : Saga<RefundSagaState>
{
    private readonly ILogger _logger;

    public RefundSaga(ILogger logger)
    {
        _logger = logger;
    }
}
```

### Fix

Add a public parameterless constructor. Sagas are configured through their state machine definition, not through constructor injection. If you need dependencies, access them through the saga's built-in service resolution.

```csharp
using Mocha.Sagas;

public class RefundSagaState : SagaStateBase
{
    public Guid OrderId { get; set; }
}

public class RefundSaga : Saga<RefundSagaState>
{
    public RefundSaga()
    {
    }
}
```

## MO0015

**Missing JsonSerializerContext for AOT**

|              |                                                                                                                                                       |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Severity** | Error                                                                                                                                                 |
| **Message**  | `MessagingModule '{0}' must specify JsonContext when publishing for AOT. Add JsonContext = typeof(YourJsonContext) to the MessagingModule attribute.` |

### Cause

The project has `PublishAot` set to `true` but the `[assembly: MessagingModule]` attribute does not include a `JsonContext` property. AOT publishing requires a `JsonSerializerContext` so the source generator can produce trim-safe serialization code for all message types.

### Example

```csharp
using Mocha;

// No JsonContext specified while targeting AOT - triggers MO0015
[assembly: MessagingModule("OrderService")]
```

### Fix

Create a `JsonSerializerContext` that includes all your message types and reference it from the `MessagingModule` attribute.

```csharp
using System.Text.Json.Serialization;
using Mocha;

[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

public record OrderPlaced(Guid OrderId);

[JsonSerializable(typeof(OrderPlaced))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
```

## MO0016

**Missing JsonSerializable attribute**

|              |                                                                                                                                                |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| **Severity** | Error                                                                                                                                          |
| **Message**  | `Type '{0}' is used as a message type but is not included in JsonSerializerContext '{1}'. Add [JsonSerializable(typeof({0}))] to the context.` |

### Cause

A type is used as a message, request, response, or saga state through a handler registration, but it is not declared via `[JsonSerializable(typeof(...))]` on the `JsonSerializerContext` specified in the `MessagingModule` attribute. Without this declaration, the AOT compiler cannot generate serialization code for the type.

### Example

```csharp
using System.Text.Json.Serialization;
using Mocha;

[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}

// OrderPlaced is missing from the context - triggers MO0016
[JsonSerializable(typeof(string))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
```

### Fix

Add a `[JsonSerializable]` attribute for every message type used by your handlers.

```csharp
using System.Text.Json.Serialization;
using Mocha;

[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => ValueTask.CompletedTask;
}

[JsonSerializable(typeof(OrderPlaced))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
```

## MO0018

**Type not in JsonSerializerContext**

|              |                                                                                                                                            |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Severity** | Warning                                                                                                                                    |
| **Message**  | `Type '{0}' is used in a {1} call but is not included in JsonSerializerContext '{2}'. Add [JsonSerializable(typeof({0}))] to the context.` |

### Cause

AOT publishing is enabled and a message type used at a call site (for example `bus.PublishAsync<T>()`) is not declared in the `JsonSerializerContext`. This is similar to [MO0016](#mo0016), but applies to types discovered at call sites rather than handler registrations. Without the declaration, the message cannot be serialized at runtime in an AOT environment.

### Example

```csharp
using System.Text.Json.Serialization;
using Mocha;

[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

public record OrderPlaced(Guid OrderId);
public record OrderShipped(Guid OrderId);

// OrderShipped is published but missing from the context - triggers MO0018
public class OrderService(IMessageBus bus)
{
    public async Task ShipOrderAsync(Guid orderId, CancellationToken ct)
    {
        await bus.PublishAsync(new OrderShipped(orderId), ct);
    }
}

[JsonSerializable(typeof(OrderPlaced))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
```

### Fix

Add a `[JsonSerializable]` attribute for every type used at a call site.

```csharp
using System.Text.Json.Serialization;
using Mocha;

[assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

public record OrderPlaced(Guid OrderId);
public record OrderShipped(Guid OrderId);

[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(OrderShipped))]
public partial class OrderServiceJsonContext : JsonSerializerContext;
```

# Mediator call-site diagnostics

These diagnostics are reported when the source generator inspects call sites that use `ISender` to dispatch commands or queries.

## MO0020

**Command/query sent but no handler found**

|              |                                                                                                         |
| ------------ | ------------------------------------------------------------------------------------------------------- |
| **Severity** | Warning                                                                                                 |
| **Message**  | `Type '{0}' is sent via {1} but no handler was found in this assembly. Ensure a handler is registered.` |

### Cause

A command or query type is dispatched via `ISender.SendAsync` or `ISender.QueryAsync`, but no corresponding handler implementation exists in the assembly. This catches cases where a call site references a message type that was never wired up with a handler.

### Example

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;

// PlaceOrder is sent but no handler exists - triggers MO0020
public class OrderController(ISender sender)
{
    public async Task CreateOrderAsync(Guid orderId, CancellationToken ct)
    {
        await sender.SendAsync(new PlaceOrder(orderId, 99.99m), ct);
    }
}
```

### Fix

Implement a handler for the message type, or remove the call site if the handler is intentionally in another assembly.

```csharp
using Mocha.Mediator;

public record PlaceOrder(Guid OrderId, decimal Total) : ICommand;

public class PlaceOrderHandler : ICommandHandler<PlaceOrder>
{
    public ValueTask HandleAsync(
        PlaceOrder command,
        CancellationToken cancellationToken)
    {
        // process the order
        return ValueTask.CompletedTask;
    }
}
```
