---
title: Introspection
---

Introspection is what enables GraphQL's rich tooling ecosystem as well powerful IDEs like [Banana Cake Pop](/products/bananacakepop) or GraphiQL.

Every GraphQL server exposes a `__schema` and `__type` field on the query type as well as an `__typename` field on each type. These fields are used to gain insights into the schema of our GraphQL server.

Using the `__schema` field, we could for example list the names of all types our GraphQL server contains:

```graphql
{
  __schema {
    types {
      name
    }
  }
}
```

We could also request the fields plus their arguments of a specific type using the `__type` field:

```graphql
{
  __type(name: "Book") {
    fields {
      name
      args {
        name
        type {
          name
        }
      }
    }
  }
}
```

The `__typename` field will most likely be the introspection feature we as regular developers will be using the most. When working with [unions](/docs/hotchocolate/v13/defining-a-schema/unions) for example it can tell us the name of the type that's being returned, allowing us to handle the result accordingly.

```graphql
{
  posts {
    __typename
    ... on VideoPost {
      videoUrl
    }
    ... on TextPost {
      text
    }
  }
}
```

While these fields can be useful to us, they are mainly intended for use in developer tooling and as regular developers we are unlikely required to write our own introspection queries on a daily basis.

[Learn more about introspection](https://graphql.org/learn/introspection)

# Disabling introspection

While introspection is a powerful feature that can tremendously improve our development workflow, it can also be used as an attack vector. A malicious user could for example request all details about all the types of our GraphQL server. Depending on the number of types this can degrade the performance of our GraphQL server. If our API should not be browsed by other developers we have the option to disable the introspection feature.

We can disable introspection by calling `AllowIntrospection()` with a `false` argument on the `IRequestExecutorBuilder`.

```csharp
services.AddGraphQLServer().AllowIntrospection(false);
```

While clients can still issue introspection queries, Hot Chocolate will now return an error response.

But we most likely do not want to disable introspection while developing, so we can make use of the `IWebHostEnvironment` to toggle introspection based on the current hosting environment.

```csharp
public class Startup
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public Startup(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AllowIntrospection(_webHostEnvironment.IsDevelopment());
    }
}
```

## Allowlisting requests

We can allow introspection on a per-request basis, while keeping it disabled for the majority of requests. In order to do this we need to create a request interceptor and determine based on the request, i.e. the `HttpContext`, whether we want to allow introspection or not.

```csharp
public class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Allow-Introspection"))
        {
            requestBuilder.AllowIntrospection();
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // We disable introspection per default
            .AllowIntrospection(false)
            .AddHttpRequestInterceptor<IntrospectionInterceptor>();
    }
}
```

[Learn more about interceptors](/docs/hotchocolate/v13/server/interceptors)

## Custom error message

If a client tries to execute an introspection query whilst introspection is not allowed, he will receive an error message similar to the following:

```json
{
  "errors": [
    {
      "message": "Introspection is not allowed for the current request.",
      "locations": [
        {
          "line": 2,
          "column": 3
        }
      ],
      "extensions": {
        "field": "__schema",
        "code": "HC0046"
      }
    }
  ]
}
```

If we need to customize the error message, we can do so in our request interceptor as well.

```csharp
public class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Allow-Introspection"))
        {
            requestBuilder.AllowIntrospection();
        }
        else
        {
            // the header is not present i.e. introspection continues
            // to be disallowed
            requestBuilder.SetIntrospectionNotAllowedMessage(
                "Missing `X-Allow-Introspection` header");
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```
