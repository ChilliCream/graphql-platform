---
title: Apollo Tracing
---

_Apollo Tracing_ is a [performance tracing specification] for _GraphQL_ servers.
It's not part of the actual _GraphQL_ [specification] itself, but there is a
common agreement in the _GraphQL_ community that this should be supported by
all _GraphQL_ servers.

> Tracing results are by default hidden in **Playground**. You have to either click on the _TRACING_ button in the bottom right corner or enable it with the `tracing.hideTracingResponse` flag in the settings.

# Enabling Apollo Tracing

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
        // Registering services / repositories here; omitted for brevity

        services.AddGraphQL(sp => SchemaBuilder.New()
          .AddQueryType<QueryType>()
          // Registering schema types and so on here; omitted for brevity
          .Create(),
          new QueryExecutionOptions
          {
              TracingPreference = TracingPreference.Always
          });
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
        // Registering services / repositories here; omitted for brevity

        services.AddGraphQL(sp => SchemaBuilder.New()
          .AddQueryType<QueryType>()
          // Registering schema types and so on here; omitted for brevity
          .Create(),
          new QueryExecutionOptions
          {
              TracingPreference = TracingPreference.OnDemand
          });
    }

    // Code omitted for brevity
}
```

Second, we have to pass an HTTP header `GraphQL-Tracing=1` on the client-side
with every query request we're interested in.

When not using the Hot Chocolate ASP.NET Core or Framework stack we have to
implement the mapping from the HTTP header to the query request property by
our self which isn't very difficult actually. See how it's solved in the
Hot Chocolate [ASP.NET Core and Framework stack].

[asp.net core and framework stack]: https://github.com/ChilliCream/hotchocolate/blob/master/src/HotChocolate/AspNetCore/src/AspNetCore.Abstractions/QueryMiddlewareBase.cs#L146-L149
[performance tracing specification]: https://github.com/apollographql/apollo-tracing
[specification]: https://facebook.github.io/graphql
