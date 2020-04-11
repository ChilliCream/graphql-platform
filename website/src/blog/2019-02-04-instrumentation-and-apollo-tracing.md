---
path: "/blog/2019/02/04/instrumentation-and-apollo-tracing"
date: "2019-02-04"
title: "GraphQL .NET Instrumentation API and Apollo Tracing"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore", "tracing"]
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

Today we have released Hot Chocolate `0.7.0`, containing one cool new feature,
we wanne talk about here, namely _Apollo Tracing_ which is extremely powerful in
identifing things like performance bottlenecks in our _GraphQL_ _APIs_ for
example. As a result, we had to enhance our general instrumentation layer, which
we all benefit from. For instance, now it's way easier to register a
_DiagnosticObserver_ and bring in your own tracing framework, respectively. In
this blog article we will focus on these two topics.

## Apollo Tracing

_Apollo Tracing_ is a [performance tracing specification] for _GraphQL_ servers.
It's not part of the actual _GraphQL_ [specification] itself, but there is a
common agreement in the _GraphQL_ community that this should be supported by
all _GraphQL_ servers.

So, we decided to introduce built-in _Apollo Tracing_ support with this version.
In order to enable _Apollo Tracing_ we just need to provide our own instance of
`QueryExecutionOptions` to the `AddGraphQL` extension method and set the
`TracingPreference` option to either `TracingPreference.Always` or
`TracingPreference.OnDemand`. The difference between these two options is
whether tracing should be enabled always which means for each request or on
demand which means per request. But for now, enough words, let's see how this
would look like in code.

```csharp
services.AddGraphQL(sp => Schema.Create(c =>
{
    // Here goes the schema definition which is omitted for brevity purpose
}),
new QueryExecutionOptions
{
    TracingPreference = TracingPreference.Always
});
```

There it is. Very simple and straightforward, right? For more information head
over [here](https://hotchocolate.io/docs/apollo-tracing). Now, let's jump over to
the next topic.

## Instrumentation API

In this version we did some heavy lifting in form of refactorings regarding the
query execution pipeline. This really helped us enhancing the
_Instrumentation_ _API_ which has been evolved in two ways. First, we increased
the amount of available diagnostic events for more fine-grained tracing
scenarios. Second, we simplified the registering of _DiagnosticObservers_ by
using _Dependancy Injection_ infrastructure. In the next example we can see how
to register a custom _DiagnosticObservers_.

```csharp
services.AddGraphQL(sp => Schema.Create(c =>
{
    // Here goes the schema definition which is omitted for brevity purpose
}),
builder =>
{
    return builder
        .UseDefaultPipeline()
        .AddDiagnosticObserver<CustomDiagnosticObserver>();
});
```

So far so good. Writing a custom _DiagnosticObservers_ is not difficult. Let's
see how we could achieve this.

```csharp
using HotChocolate.Execution;
using Microsoft.Extensions.DiagnosticAdapter;

namespace CustomNamespace
{
    internal class CustomDiagnosticObserver
        : IDiagnosticObserver
    {
        [DiagnosticName("HotChocolate.Execution.Query")]
        public void QueryExecute()
        {
            // This method is required to enable recording "Query.Start" and
            // "Query.Stop" diagnostic events. Do not write code in here.
        }

        [DiagnosticName("HotChocolate.Execution.Query.Start")]
        public void BeginQueryExecute(IQueryContext context)
        {
            // Here goes your code to trace begin query execution events.
        }

        [DiagnosticName("HotChocolate.Execution.Query.Stop")]
        public void EndQueryExecute(
            IQueryContext context,
            IExecutionResult result)
        {
            // Here goes your code to trace end query execution events.
        }
    }
}
```

In the above example we showed you just a few diagnostic events. Head over
[here](https://hotchocolate.io/docs/instrumentation) for a complete list of
diagnostic events.

We hope you enjoyed reading and be welcome to let us know what you think about
it in the comments section. Thank you!

[performance tracing specification]: https://github.com/apollographql/apollo-tracing
[specification]: https://facebook.github.io/graphql
