---
path: "/blog/2019/03/19/logging-with-hotchocolate"
date: "2019-03-19"
title: "GraphQL - Tracing with Hot Chocolate"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore", "tracing"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

One common question that comes up on our slack channel is if Hot Chocolate supports some kind of logging infrastructure. My personal opinion here is that logging/tracing is often very project specific and an API should not force one specific logging solution onto its users.

Instead we have opted to provide diagnostic events through Microsoft`s diagnostic source which does not force us to serialize any payloads. This in turn gives you the ability to pick and choose the information that best fits your need for your tracing/logging solution.

This post will walk you through on how to add a logger of your choice to Hot Chocolate and get exactly the right amount of information for your project.

In this blog we will use the ASP.Net core logging API to show how a logger can be attached to our diagnostic events.

## Setup

But before we can get started let us first setup a web project with Hot Chocolate:

```bash
mkdir logging
cd logging
dotnet new web
dotnet add package HotChocolate.AspNetCore:9.0.0-preview.5
dotnet add package HotChocolate.AspNetCore.Playground:9.0.0-preview.5
```

## Configure the Logger

After our project is setup let us start with setting up the ASP.net core logging infrastructure. This is fairly easy with ASP.net core. Head over to the `Program.cs` and replace the builder configuration with the following one.

```csharp
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        })
        .UseStartup<Startup>();
```

`ConfigureLogging` configures the various logging providers that are then available throughout our `WebHost`. In our simple example we clear all the providers and then add only the console logger.

Next head over to the `Startup.cs` and register the logger with the dependency injection by adding the following line to `ConfigureServices`.

```csharp
services.AddLogging();
```

Perfect, now we have setup all the basics and can get started.

## Diagnostic Observer

The Hot Chocolate server provides diagnostic events through a diagnostic source. We can subscribe to these events by providing a diagnostic observer. A diagnostic observer is basically any class that implements our marker interface `IDiagnosticObserver`.

Into this class we can add public methods that are subscribed to the actual diagnostic listener. The methods that shall subscribe to an event have to be annotated to with the `DiagnosticNameAttribute`.

We have listed the various available events and their payloads in our documentation that can be found [here](https://hotchocolate.io/docs/next/instrumentation).

Let us say that in our case we want to write a message to the console whenever a request begins. Moreover, if the request is a query or mutation then we also want to write the result to the console.

Before we add the actual event methods, let us create a class called `DiagnosticObserver`. In order to write events to the logger we need to inject a concrete logger to our class. So, our class could look like the following:

```csharp
public class DiagnosticObserver
    : IDiagnosticObserver
{
    private readonly ILogger _logger;

    public DiagnosticObserver(ILogger<DiagnosticObserver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

Next, let us add our two event methods.

```csharp
public class DiagnosticObserver
        : IDiagnosticObserver
{
    private readonly ILogger _logger;

    public DiagnosticObserver(ILogger<DiagnosticObserver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [DiagnosticName("HotChocolate.Execution.Query")]
    public void OnQuery(IQueryContext context)
    {
        // This method is used as marker to enable begin and end events
        // in the case that you want to explicitly track the start and the
        // end of this event.
    }

    [DiagnosticName("HotChocolate.Execution.Query.Start")]
    public void BeginQueryExecute(IQueryContext context)
    {
        _logger.LogInformation(context.Request.Query);
    }

    [DiagnosticName("HotChocolate.Execution.Query.Stop")]
    public void EndQueryExecute(IQueryContext context)
    {
        if(context.Result is IReadOnlyQueryResult result)
        {
            using (var stream = new MemoryStream())
            {
                var resultSerializer = new JsonQueryResultSerializer();
                resultSerializer.SerializeAsync(
                    result, stream).Wait();
                _logger.LogInformation(
                    Encoding.UTF8.GetString(stream.ToArray()));
            }
        }
    }
}
```

In order to enable start/stop events we have to add a third method that represents the subscription to the event.

This is only needed when subscribing to activities that consist of a start event and a stop event. These start and stop events allow for measuring performance.

Apart from our standard payloads that are described in our documentation we can also inject the `Activity` instance to your start/stop event and use the high precision time measurement that the diagnostics APIs provide.

The events always provide you with the full context objects that are available in the query and field middleware pipeline. You basically have full access to all the data that you would have access to in a middleware and by this you are able to pick the information you need for your tracing/logging solution and create the logging messages in a structure that fits your needs.

Moreover, you also can use the `ContextData` dictionary on the context objects to share information between your subscription events like a request identifier.

After we have implemented our observer, we have to register it as a service.

Add the following line to the `ConfigureServices` method in our `Startup.cs`.

```csharp
services.AddDiagnosticObserver<DiagnosticObserver>();
```

With that our logger is ready to receive events. We now just need a GraphQL API that produces events.

For this we add a simple query type:

```csharp
public class Query
{
    public string Hello() => "world";
}
```

Next we register the query type with our schema by adding the following line to the `ConfigureServices` method in our `Startup.cs`.

```csharp
services.AddGraphQL(c =>
{
    c.RegisterQueryType<Query>();
});
```

Last but not least we have to add our `GraphQL` middleware and in order to write some queries our `Playground` middleware.

Replace the `Configure` method in our `Startup.cs` with the following:

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseGraphQL();
    app.UsePlayground();

    app.Run(async (context) =>
    {
        await context.Response.WriteAsync("Hello World!");
    });
}
```

You should now be able to start the GraphQL server.

```bash
dotnet run
```

The server should be accessible through plaground under the following URL `http://127.0.0.1:5000/playground`.

Add the following query and execute it:

```graphql
{
  hello
}
```

The query and the result should now be printed to your console.

This is just a simple example of how to subscribe to our diagnostic events. Checkout our documentation for a list of all of the events available [here](https://hotchocolate.io/docs/next/instrumentation).

We have added this example project to our example repo [here](https://github.com/ChilliCream/hotchocolate-examples/tree/master/Instrumentation).

Also, we have a more complex implementation of a Hot Chocolate ETW event source [here](https://github.com/ChilliCream/thor-client/tree/master/src/Clients/HotChocolate).

Another example is our [Apollo Tracing implementation](https://github.com/ChilliCream/hotchocolate/blob/master/src/Core/Core/Execution/Instrumentation/ApolloTracingDiagnosticObserver.cs) that is also based on our instrumentation API.

I hope this little field trip into our instrumentation API gives a little outlook of an often-overlooked feature that is coming with version 9. All of what I showed in this blog is available with preview 5 (9.0.0-preview.5) that we released today.

We will add stitching related events with the next view preview builds.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
