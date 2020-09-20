---
title: Instrumentation
---

The Hot Chocolate instrumentation allows you to receive internal instrumentation events and process them further. Instrumentation events are provided through a `DiagnosticSource`.

Using Microsoft\`s `DiagnosticSource` API allows us to provide rich events without compromising on information detail.

As a developer using Hot Chocolate we can subscribe to those events and delegate them either to our logging provider or to another tracing infrastructure for further processing.

This allows us to just take the information we need for a certain logging solution and for instance craft the events provided by Hot Chocolate into logging messages that fit our project.

# Events

First let us have a look at what events Hot Chocolate currently provides and what they mean. Later we will walk you through how to setup an `IDiagnosticObserver`.

## Query Events

Query events are raised per request. This means that for each query request that we fire up against a Hot Chocolate one query event is raised.

The following query events are available:

## Start Query

The start event is raised once the query engine receives a request.

```csharp
[DiagnosticName("HotChocolate.Execution.Query.Start")]
public void BeginQueryExecute(IQueryContext context)
{
    // ... your code
}
```

The query context that we provide as payload with the event is the full query context on which the query middleware operates. This enables us to pick and choose the information that we want.

## Stop Query

The stop event is raised once the query engine has completed processing the request. This event is even called if an error has occurred. Additional to the `IQueryContext` the event also provides the `IExecutionResult`.

```csharp
[DiagnosticName("HotChocolate.Execution.Query.Stop")]
public void EndQueryExecute(
    IQueryContext context,
    IExecutionResult result)
{
    // ... your code
}
```

## Query Error

The error event is raised should there be an unhandled exception on the query middleware level. This event is not raised whenever a validation or field error is thrown.

```csharp
[DiagnosticName("HotChocolate.Execution.Query.Error")]
public virtual void OnQueryError(
    IQueryContext context,
    Exception exception)
{
    // ... your code
}
```

## Parser Events

The parser events are raised when the parser middleware is invoked. It is important to know that the Hot Chocolate server caches queries. This means that only the first time a query is executed, we can measure the parsing time.

## Start Parsing

The start event is raised once the parser middleware is invoked.

```csharp
[DiagnosticName("HotChocolate.Execution.Parsing.Start")]
public void BeginParsing(IQueryContext context)
{
    // ... your code
}
```

## Stop Parsing

The stop event is raised once the parser finished. It is important to know that the stop event is even raised if a `SyntaxException` is thrown. The `Document` property on the `IQueryContext` will be null in this case. The parser middleware will add a property to the context data indicating if the query was retrieved from the cache: `DocumentRetrievedFromCache`.

```csharp
[DiagnosticName("HotChocolate.Execution.Parsing.Stop")]
public void EndParsing(IQueryContext context)
{
    // ... your code
}
```

## Parsing Errors

The parser will throw a `SyntaxException` if the query is not syntactically correct. The `SyntaxException` will cause a query error.

## Validation Events

The validation events are raised whenever the validation middleware is invoked. Like with the parsing middleware the validation middleware will cache validation results. This means that only the first validation of a query document can be used to measure the validation duration. The context property `DocumentRetrievedFromCache` can also be used in this case to detect if the validation result was pulled from the internal cache or if it was computed.

## Validation Start

The validation start event is called once the validation middleware is invoked.

```csharp
[DiagnosticName("HotChocolate.Execution.Validation.Start")]
public void BeginValidation(IQueryContext context)
{
    // ... your code
}
```

## Validation Stop

The stop event is raised once the validation finished. It is important to know that the stop event is even raised if a validation error is raised.

```csharp
[DiagnosticName("HotChocolate.Execution.Validation.Stop")]
public void EndValidation(IQueryContext context)
{
    // ... your code
}
```

## Validation Errors

The validation error event will be raised for each query document analysis that yields at least one error.

```csharp
[DiagnosticName("HotChocolate.Execution.Validation.Error")]
public void OnValidationError(
    IQueryContext context,
    IReadOnlyCollection<IError> errors)
{
    // ... your code
}
```

## Operation Events

Operation events represent the execution of the operation by the query engine. At this point all information about the operation have been resolved and can be accessed.

The following operation events are available:

## Start Operation

```csharp
[DiagnosticName("HotChocolate.Execution.Operation.Start")]
public void BeginOperationExecute(IQueryContext context)
{
    // ... your code
}
```

## Stop Operation

```csharp
[DiagnosticName("HotChocolate.Execution.Operation.Stop")]
public void EndOperationExecute(
    IQueryContext context,
    IExecutionResult result)
{
    // ... your code
}
```

## Resolver Events

Resolver events are raised for every single resolver that is invoked. This is the perfect event to subscribe to if you want to add performance analysis or other resolver tracing solutions.

> Have a look at our [ApolloTracingDiagnosticObserver](https://github.com/ChilliCream/hotchocolate/blob/master/src/Core/Core/Execution/Instrumentation/ApolloTracingDiagnosticObserver.cs) to get an idea of how to implement a performance analysis solution.

## Resolver Start

The resolver start event is raised for each invocation of a resolver pipeline. The resolver pipeline is made-up of multiple field middleware components. The exact composition of such a pipeline varies on your setup.

```csharp
[DiagnosticName("HotChocolate.Execution.Resolver.Start")]
public void BeginResolveField(IResolverContext context)
{
    // ... your code
}
```

## Resolver Stop

The resolver stop event is raised once the execution of the resolver pipeline is completed. The provided result is the not completed result of the resolver. This means that the actual result that is integrated into the query result can differ since type converter and serialization are applied during field value completion.

```csharp
[DiagnosticName("HotChocolate.Execution.Resolver.Stop")]
public void EndResolveField(
    IResolverContext context,
    object result)
{
    // ... your code
}
```

## Resolver Error

The resolver error event is raised should one or more resolver errors occurs.

```csharp
[DiagnosticName("HotChocolate.Execution.Resolver.Error")]
public void OnResolverError(
    IResolverContext context,
    IEnumerable<IError> errors)
{
    // ... your code
}
```

# How to subscribe

In order to subscribe to the Hot Chocolate instrumentation events, you have to create a class that implements the marker interface `IDiagnosticObserver`.

```csharp
public class MyDiagnosticObserver
    : IDiagnosticObserver
{
}
```

The observer subscribes to an event by adding a method that is annotated with the event name like the following:

```csharp
public class MyDiagnosticObserver
    : IDiagnosticObserver
{
    [DiagnosticName("HotChocolate.Execution.Validation.Error")]
    public void OnValidationError(
        IQueryContext context,
        IReadOnlyCollection<IError> errors)
    {
        // ... your code
    }
}
```

When subscribing to start/stop events you also have to add the actual event method, otherwise the diagnostic source will not enable the event.

```csharp
public class MyDiagnosticObserver
    : IDiagnosticObserver
{
    [DiagnosticName("HotChocolate.Execution.Query")]
    public void OnQuery(IQueryContext context)
    {
        // This method is used to enable start/stop events for query.
    }

    [DiagnosticName("HotChocolate.Execution.Query.Start")]
    public void BeginQueryExecute(IQueryContext context)
    {
        // ... your code
    }

    [DiagnosticName("HotChocolate.Execution.Query.Stop")]
    public void EndQueryExecute(
        IQueryContext context,
        IExecutionResult result)
    {
        // ... your code
    }
}
```

You can use the context data to pass tracing details like a custom request id between your events:

```csharp
public class MyDiagnosticObserver
    : IDiagnosticObserver
{
    [DiagnosticName("HotChocolate.Execution.Query")]
    public void OnQuery(IQueryContext context)
    {
        // This method is used to enable start/stop events for query.
    }

    [DiagnosticName("HotChocolate.Execution.Query.Start")]
    public void BeginQueryExecute(IQueryContext context)
    {
        context.ContextData["TracingId"] = Guid.NewGuid();
        // ... your code
    }

    [DiagnosticName("HotChocolate.Execution.Query.Stop")]
    public void EndQueryExecute(
        IQueryContext context,
        IExecutionResult result)
    {
        Guid tracingId = (Guid)context.ContextData["TracingId"];
        // ... your code
    }
}
```

There are two ways to register the diagnostics observer with the execution engine. You can either register the observer with the executor directly through the `QueryExecutionBuilder` or you can add the diagnostic observer to your dependency injection provider.

Registering the observer with the `QueryExecutionBuilder` does not require any dependency injection provider, but let`s you only inject infrastructure services.

```csharp
SchemaBuilder.New()
    ...
    .Create()
    .MakeExecutable(builder => builder
        .AddDiagnosticObserver<MyDiagnosticObserver>()
        .UseDefaultPipeline());
```

If you want to use the observer in conjunction with your dependency injection provider you can also add the observer to your services. We have added an extension method for `IServiceCollection` that mirrors the builder extension.

```csharp
services.AddDiagnosticObserver<MyDiagnosticObserver>();
```

If you are registering the diagnostics observer with the dependency injection you have to ensure that the resulting service provider is registered with the schema.

If you are using our `AddGraphQL` or `AddStitchedSchema` extensions, you should be covered. In the case that you are putting everything together yourself you will need to register the service provider with your schema manually.

```csharp
service.AddSingleton(sp => SchemaBuilder.New()
    .AddServices(sp)
    ...
    .Create());

QueryExecutionBuilder
    .New()
    .UseDefaultPipeline()
    .Populate(services);
```

# Examples

We have created a little example project that demonstrates how you can delegate Hot Chocolate events to the ASP.NET core logger API.

[ASP.NET ILogger Example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/Instrumentation)

We also have an implementation that we use in production that builds upon Microsoft\`s ETW. This is a more complex example since there is a lot of `unsafe` code.

[ETW Example](https://github.com/ChilliCream/thor-client/tree/master/src/Clients/HotChocolate)

# Blogs

[Tracing with Hot Chocolate](https://chillicream.com/blog/2019/03/19/logging-with-hotchocolate)
