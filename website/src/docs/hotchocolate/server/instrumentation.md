---
title: Instrumentation
---

With Hot Chocolate we can create custom diagnostic event listeners, allowing us to hook into internal instrumentation events and further process them.

We can subscribe to these events and delegate them either to our logging provider or to another tracing infrastructure. We are free to gather data only on one event or all of them, allowing us to craft tracing behavior that fits the need of our project.

# Usage

We can register diagnostic event listeners by calling `AddDiagnosticEventListener` on the `IRequestExecutorBuilder`.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDiagnosticEventListener<MyExecutionEventListener>();
    }
}
```

Currently there are diagnostic event listeners for the following event types:

- [Execution events](#execution-events)
- [DataLoader events](#dataloader-events)

We can inject services into the diagnostic event listeners to access them in the specific event handlers. Please note that injected services are effectively singleton, since the diagnostic event listener is only instantiated once.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<MyExecutionEventListener> _logger;

    public MyExecutionEventListener(ILogger<MyExecutionEventListener> logger)
        => _logger = logger;

    public override void RequestError(IRequestContext context,
        Exception exception)
    {
        _logger.LogError(exception, "A request error occured!");
    }
}
```

> ⚠️ Note: Diagnostic event handlers are executed synchronously as part of the GraphQL request. Long running operations inside of a diagnostic event handler will negatively impact the query performance. Expensive operations should only be enqueued from within the handler and processed by a background service.

## Scopes

Most diagnostic event handlers have a return type of `void`, but some return an `IDisposable`. These are event handlers that enclose a specific operation like a scope. The scope is instantiated at the start of the operation and disposed at the end of the operations.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<MyExecutionEventListener> _logger;

    public MyExecutionEventListener(ILogger<MyExecutionEventListener> logger)
        => _logger = logger;

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var start = DateTime.UtcNow;

        return new RequestScope(start, _logger);
    }
}

public class RequestScope : IDisposable
{
    private readonly ILogger _logger;
    private readonly DateTime _start;

    public RequestScope(DateTime start, ILogger logger)
    {
        _start = start;
        _logger = logger;
    }

    public void Dispose()
    {
        var end = DateTime.UtcNow;
        var elapsed = end - _start;

        _logger.LogInformation("Request finished after {Ticks} ticks",
            elapsed.Ticks);
    }
}
```

If we are not interested in the scope of a specific diagnostic event handler, we can simply return an `EmptyScope`.

```csharp
public override IDisposable ExecuteRequest(IRequestContext context)
{
    _logger.LogInformation("Request execution started!");

    return EmptyScope;
}
```

# Execution Events

We can hook into execution events of the Hot Chocolate execution engine by creating a class inheriting from `ExecutionDiagnosticEventListener`.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        // Omitted code for brevity
    }
}
```

The following methods can be overriden.

| Method name                  | Description |
| ---------------------------- | ----------- |
| ValidateDocument             |             |
| ValidationErrors             |             |
| AddedDocumentToCache         |             |
| AddedOperationToCache        |             |
| DispatchBatch                |             |
| ExecuteRequest               |             |
| ExecuteSubscription          |             |
| ExecutorEvicted              |             |
| ExecutorCreated              |             |
| OnSubscriptionEvent          |             |
| ParseDocument                |             |
| RequestError                 |             |
| ResolveFieldValue            |             |
| ResolverError                |             |
| RetrievedDocumentFromCache   |             |
| RetrievedDocumentFromStorage |             |
| RetrievedOperationFromCache  |             |
| RunTask                      |             |
| StartProcessing              |             |
| StopProcessing               |             |
| SubscriptionEventResult      |             |
| SubscriptionEventError       |             |
| SubscriptionTransportError   |             |
| SyntaxError                  |             |
| TaskError                    |             |

# DataLoader Events

We can hook into DataLoader events by creating a class inheriting from `ExecutionDiagnosticEventListener`.

```csharp
public class MyDataLoaderEventListener : DataLoaderDiagnosticEventListener
{
    public override IDisposable ExecuteBatch<TKey>(IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        // Omitted code for brevity
    }
}
```

The following methods can be overriden.

| Method name           | Description |
| --------------------- | ----------- |
| BatchError            |             |
| BatchItemError        |             |
| BatchResults          |             |
| ExecuteBatch          |             |
| ResolvedTaskFromCache |             |

# Apollo Tracing

<!-- todo: rework -->

_Apollo Tracing_ is a [performance tracing specification] for _GraphQL_ servers.
It's not part of the actual _GraphQL_ specification itself, but there is a
common agreement in the _GraphQL_ community that this should be supported by
all _GraphQL_ servers.

> Tracing results are by default hidden in **Playground**. You have to either click on the _TRACING_ button in the bottom right corner or enable it with the `tracing.hideTracingResponse` flag in the settings.

## Enabling Apollo Tracing

Due to built-in _Apollo Tracing_ support it's actually very simple to enable
this feature. There is an option named `TracingPreference` which takes one of
three states. In the following table we find all of these states explained.

| Key        | Description                                                                                                                    |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `Never`    | _Apollo Tracing_ is disabled; this is the default value.                                                                       |
| `OnDemand` | _Apollo Tracing_ is enabled partially which means that it traces only by passing a special header to a specific query request. |
| `Always`   | _Apollo Tracing_ is enabled completely which means all query requests will be traced automatically.                            |

When creating your GraphQL schema, we just need to add an additional option
object to enable _Apollo Tracing_. By default, as explained in the above table
_Apollo Tracing_ is disabled. Let's take a look at the first example which
describes how _Apollo Tracing_ is enabled permanently.

**Enable _Apollo Tracing_ permanently**

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()

            // this adds apollo tracing
            .AddApolloTracing(TracingPreference.Always);
    }

    // Code omitted for brevity
}
```

By setting the `TracingPreference` to `TracingPreference.Always`, we enabled
_Apollo Tracing_ permanently; nothing else to do here. Done.

**Enable _Apollo Tracing_ per query request**

First, we need to enable _Apollo Tracing_ on the server-side. It's almost
identical to the above example.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()

            // this adds apollo tracing
            .AddApolloTracing(TracingPreference.OnDemand);
    }

    // Code omitted for brevity
}
```

Second, we have to pass an HTTP header `GraphQL-Tracing=1` or `X-Apollo-Tracing=1` on the client-side
with every query request we're interested in.

When not using the Hot Chocolate ASP.NET Core or Framework stack we have to
implement the mapping from the HTTP header to the query request property by
our self which isn't very difficult actually. See how it's solved in the
Hot Chocolate [ASP.NET Core and Framework stack].

[asp.net core and framework stack]: https://github.com/ChilliCream/hotchocolate/blob/master/src/HotChocolate/AspNetCore/src/AspNetCore.Abstractions/QueryMiddlewareBase.cs#L146-L149
[performance tracing specification]: https://github.com/apollographql/apollo-tracing
[specification]: https://facebook.github.io/graphql
