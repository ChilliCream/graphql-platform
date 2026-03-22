---
title: Introspection
---

Introspection is what enables GraphQL's rich tooling ecosystem and powerful IDEs like [Nitro](/products/nitro) or GraphiQL.

Every GraphQL server exposes a `__schema` and `__type` field on the query type as well as a `__typename` field on each type. These fields provide insights into the schema of your GraphQL server.

Using the `__schema` field, you could list the names of all types your GraphQL server contains:

```graphql
{
  __schema {
    types {
      name
    }
  }
}
```

You could also request the fields plus their arguments of a specific type using the `__type` field:

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

The `__typename` field is the introspection feature you will use the most in day-to-day development. When working with [unions](/docs/hotchocolate/v16/building-a-schema/unions), for example, it tells you the name of the type being returned, letting you handle the result accordingly.

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

While these fields can be useful to you directly, they are mainly intended for developer tooling. You are unlikely to write your own introspection queries on a daily basis.

[Learn more about introspection](https://graphql.org/learn/introspection)

# Disabling Introspection

While introspection is a powerful feature that can improve your development workflow, it can also be used as an attack vector. A malicious user could request all details about all types in your GraphQL server. Depending on the number of types, this can degrade performance. If your API should not be browsable by other developers, you have the option to disable introspection.

Disable introspection by calling `AllowIntrospection()` with a `false` argument on the `IRequestExecutorBuilder`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AllowIntrospection(false);
```

While clients can still issue introspection queries, Hot Chocolate returns an error response.

You most likely do not want to disable introspection while developing, so you can toggle it based on the current hosting environment:

```csharp
builder.Services
    .AddGraphQLServer()
    .AllowIntrospection(builder.Environment.IsDevelopment());
```

## Allowlisting Requests

You can allow introspection on a per-request basis while keeping it disabled for the majority of requests. Create a request interceptor and determine based on the request (the `HttpContext`) whether to allow introspection.

```csharp
public class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
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
```

```csharp
builder.Services
    .AddGraphQLServer()
    // Disable introspection by default
    .AllowIntrospection(false)
    .AddHttpRequestInterceptor<IntrospectionInterceptor>();
```

[Learn more about interceptors](/docs/hotchocolate/v16/server/interceptors)

## Custom Error Message

If a client tries to execute an introspection query when introspection is not allowed, they receive an error message similar to the following:

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

If you need to customize the error message, do so in your request interceptor:

```csharp
public class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Allow-Introspection"))
        {
            requestBuilder.AllowIntrospection();
        }
        else
        {
            // the header is not present, introspection continues
            // to be disallowed
            requestBuilder.SetIntrospectionNotAllowedMessage(
                "Missing `X-Allow-Introspection` header");
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

# Next Steps

- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for per-request customization.
- [Security](/docs/hotchocolate/v16/security) for a broader look at securing your GraphQL server.
- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for configuring the Nitro IDE.
