---
title: Instrumentation
---

Hot Chocolate allows us to create custom diagnostic event listeners, allowing us to hook into internal instrumentation events and further process them.

We can subscribe to these events and delegate them either to our logging provider or to another tracing infrastructure. We are free to gather data only on one event or all of them, allowing us to craft tracing behavior that fits the need of our project.

# Diagnostic events

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

Most diagnostic event handlers have a return type of `void`, but some return an `IDisposable`. These are event handlers that enclose a specific operation, sort of like a scope. This scope is instantiated at the start of the operation and disposed at the end of the operation.

To create a scope we can simply create a class implementing `IDisposable`.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<MyExecutionEventListener> _logger;

    public MyExecutionEventListener(ILogger<MyExecutionEventListener> logger)
        => _logger = logger;

    // this is invoked at the start of the `ExecuteRequest` operation
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

    // this is invoked at the end of the `ExecuteRequest` operation
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

## Execution Events

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

| Method name                  | Description                                                                                                                                    |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| ExecuteRequest               | Scope that encloses the entire GraphQL request execution. Also the first diagnostic event raised during a GraphQL request.                     |
| RequestError                 | Called if the GraphQL request produced an error. Called immediately before the scope of `ExecuteRequest` is disposed.                          |
| ExecuteSubscription          | Scope that encloses the execution of a subscription query. Scope is created once a client subscribes and disposed once the subscription ends.  |
| ParseDocument                | Scope that encloses the parsing of a document.                                                                                                 |
| SyntaxError                  | Called if a document could not be parsed due to a syntax error.                                                                                |
| ValidateDocument             | Scope that encloses the validation of a document.                                                                                              |
| ValidationErrors             | Called if errors occured during the validation of the document.                                                                                |
| StartProcessing              | Scope that encloses the scheduling of some work, e.g. invoking a DataLoader or starting execution tasks.                                       |
| StopProcessing               | Called if the execution engine has to wait for resolvers to complete or whenever the execution has completed.                                  |
| RunTask                      | Scope that encloses the execution of an execution task. A `ResolverExecutionTask` uses the `ResolveFieldValue` event instead.                  |
| TaskError                    | Called if an execution task produced an error.                                                                                                 |
| ResolveFieldValue            | Scope that encloses the execution of a specific field resolver. (\*)                                                                           |
| ResolverError                | Called if a specific field resolver produces an error.                                                                                         |
| OnSubscriptionEvent          | Scope that encloses the computation of a subscription result, once the event stream has yielded a new payload.                                 |
| SubscriptionEventResult      | Called once the subscription result has been successfully computed.                                                                            |
| SubscriptionEventError       | Called if the computation of the subscription result produced an error.                                                                        |
| SubscriptionTransportError   | Called if a subscription result could not be delivered to a client due to a transport issue.                                                   |
| AddedDocumentToCache         | Called once a document has been added to `DocumentCache`.                                                                                      |
| RetrievedDocumentFromCache   | Called once a document has been retrieved from the `DocumentCache`.                                                                            |
| AddedOperationToCache        | Called once an operation has been added to the `OperationCache`.                                                                               |
| RetrievedOperationFromCache  | Called once an operation has been retrieved from the `OperationCache`.                                                                         |
| RetrievedDocumentFromStorage | Called once a document has been retrieved from a persisted query storage.                                                                      |
| ExecutorCreated              | Called once a request executor has been created. Executors are created once for a schema (includes stitched schemas) during the first request. |
| ExecutorEvicted              | Called once a request executor is evicted. This can happen if the schema or the configuration of the executor changes.                         |

(\*): The `ResolveFieldValue` event is not invoked per default, as it would be too much overhead to execute the event for each resolver used within a query. We have to override the `EnableResolveFieldValue` property in order for the execution engine to start invoking the event handler.

```csharp
public class MyExecutionEventListener : ExecutionDiagnosticEventListener
{
    public override bool EnableResolveFieldValue => true;

    public override IDisposable ResolveFieldValue(IMiddlewareContext context)
    {
        // Omitted code for brevity
    }
}
```

### Caching

It's important to note that some diagnostic events are executed differently based on whether caching is involved or not.

The `ParseDocument` event for example is only executed once for the same document. For subsequent requests using the same document the document is no longer parsed, i.e. the diagnostic event is not invoked.

While the `ValidateDocument` event is raised for each request, it only makes sense to measure the first validation. For subsequent requests the validation result might have been cached, as indicated by the `IRequestContext.IsCachedDocument` property.

```csharp
public override IDisposable ValidateDocument(IRequestContext context)
{
    if (!context.IsCachedDocument)
    {
        // only profile the validation if the result has not been cached
    }

    return EmptyScope;
}
```

## DataLoader Events

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

| Method name           | Description                                                                                                     |
| --------------------- | --------------------------------------------------------------------------------------------------------------- |
| ExecuteBatch          | Scope that encloses a batch operation, i.e. the resolution of a specific set of keys.                           |
| BatchResults          | Called once a batch operation has been completed, i.e. all items for a specific set of keys have been resolved. |
| BatchError            | Called if a batch operation has failed.                                                                         |
| BatchItemError        | Called for a specific item that contained an error within a batch operation.                                    |
| ResolvedTaskFromCache | Called once a task to resolve an item by its key has been added or retrieved from the `TaskCache`.              |

# Apollo Tracing

_Apollo Tracing_ is a [performance tracing specification](https://github.com/apollographql/apollo-tracing) for GraphQL servers. It works by returning tracing information about the current request alongside the computed data. While it is not part of the GraphQL specification itself, there is a common agreement in the GraphQL community that it should be supported by all GraphQL servers.

**Example**

```graphql
{
  book(id: 1) {
    name
    author
  }
}
```

The above request would result in the below response, if _Apollo Tracing_ is enabled.

```json
{
  "data": {
    "book": {
      "name": "C# in Depth",
      "author": "Jon Skeet"
    }
  },
  "extensions": {
    "tracing": {
      "version": 1,
      "startTime": "2021-09-25T15:31:41.6515774Z",
      "endTime": "2021-09-25T15:31:43.1602255Z",
      "duration": 1508648100,
      "parsing": { "startOffset": 13335, "duration": 781 },
      "validation": { "startOffset": 17012, "duration": 323681 },
      "execution": {
        "resolvers": [
          {
            "path": ["book"],
            "parentType": "Query",
            "fieldName": "book",
            "returnType": "Book",
            "startOffset": 587048,
            "duration": 1004748344
          },
          {
            "path": ["book", "author"],
            "parentType": "Book",
            "fieldName": "author",
            "returnType": "String",
            "startOffset": 1005854823,
            "duration": 500265020
          }
        ]
      }
    }
  }
}
```

## Enabling Apollo Tracing

_Apollo Tracing_ needs to be expicitly enabled by caling `AddApolloTracing` on the `IRequestExecutorBuilder`.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddApolloTracing();
    }
}
```

Further we can specify a `TracingPreference`. Per default it is `TracingPreference.OnDemand`.

```csharp
services
    .AddGraphQLServer()
    .AddApolloTracing(TracingPreference.Always);
```

There are three possible options for the `TracingPreference`.

| Option     | Description                                                                                   |
| ---------- | --------------------------------------------------------------------------------------------- |
| `Never`    | _Apollo Tracing_ is disabled. Useful if we want to conditionally disable _Apollo Tracing_.    |
| `OnDemand` | _Apollo Tracing_ only traces requests, if a specific header is passed with the query request. |
| `Always`   | _Apollo Tracing_ is always enabled and all query requests are traced automatically.           |

## On Demand

When _Apollo Tracing_ is added using the `TracingPreference.OnDemand`, we are required to pass one of the following HTTP headers with our query request in order to enable tracing for this specific request.

- `GraphQL-Tracing=1`
- `X-Apollo-Tracing=1`

When using `curl` this could look like the following.

```bash
curl -X POST -H 'GraphQL-Tracing: 1' -H 'Content-Type: application/json' \
    -d '{"query":"{\n  book(id: 1) {\n    name\n    author\n  }\n}\n"}' \
    'http://localhost:5000/graphql'
```
