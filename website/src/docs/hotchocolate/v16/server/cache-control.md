---
title: Cache Control
description: Learn how to configure Cache-Control and Vary response headers for CDN and HTTP caching in Hot Chocolate GraphQL servers using @cacheControl directives, UseQueryCache, and cache-control options.
---

Cache control defines how HTTP clients, browsers, reverse proxies, and CDNs should cache GraphQL responses.

This chapter explains HTTP cache semantics first, then shows how GraphQL `@cacheControl` metadata is translated into `Cache-Control` and `Vary` response headers by Hot Chocolate.

# What Cache Control Means in HTTP

`Cache-Control` is an HTTP response header that tells caches whether a response can be stored and how long it remains fresh.

Common directives:

| Directive            | Meaning                                       |
| -------------------- | --------------------------------------------- |
| `max-age=<seconds>`  | Response freshness lifetime.                  |
| `s-maxage=<seconds>` | Shared-cache freshness lifetime (for CDNs).   |
| `public`             | Shared caches may store this response.        |
| `private`            | Shared caches should not store this response. |
| `Vary: <header>`     | Cache key depends on request headers.         |

For GraphQL APIs, these headers matter because one endpoint serves many operations and cache behavior should still be explicit and predictable.

# How GraphQL `@cacheControl` Works

GraphQL uses `@cacheControl` to declare cache intent on fields and types.

## GraphQL SDL Example

```graphql
type Query {
  productById(id: ID!): Product @cacheControl(maxAge: 300, sharedMaxAge: 900)

  me: UserProfile
    @cacheControl(maxAge: 60, scope: PRIVATE, vary: ["Authorization"])
}
```

## Hot Chocolate Resolver Example

```csharp
using HotChocolate.Caching;

public sealed class Query
{
    [CacheControl(300, SharedMaxAge = 900)]
    public Product? ProductById(int id)
        => ProductStore.GetById(id);

    [CacheControl(60, Scope = CacheControlScope.Private, Vary = ["Authorization"])]
    public UserProfile Me()
        => UserStore.GetCurrent();
}
```

At execution time, Hot Chocolate computes one effective cache policy for the full response.

# Enable Cache Control in Hot Chocolate

Install the package:

```bash
dotnet add package HotChocolate.Caching
```

Register schema support and response-header middleware:

```csharp
using HotChocolate.Caching;

builder
    .AddGraphQL()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(o =>
    {
        o.ApplyDefaults = false;
    });
```

- `AddCacheControl()` registers the `@cacheControl` directive and cache-constraint computation.
- `UseQueryCache()` writes `Cache-Control` and `Vary` values to HTTP responses.

# Cache-Control Options

`ModifyCacheControlOptions` configures default behavior:

```csharp
builder
    .AddGraphQL()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(o =>
    {
        o.Enable = true;
        o.DefaultMaxAge = 60;
        o.DefaultScope = CacheControlScope.Public;
        o.ApplyDefaults = true;
    });
```

| Option          | Type                | Default  | Description                                                           |
| --------------- | ------------------- | -------- | --------------------------------------------------------------------- |
| `Enable`        | `bool`              | `true`   | Enables cache-control response handling.                              |
| `DefaultMaxAge` | `int`               | `0`      | Default `max-age` when `ApplyDefaults` is enabled.                    |
| `DefaultScope`  | `CacheControlScope` | `Public` | Default cache scope when `ApplyDefaults` is enabled.                  |
| `ApplyDefaults` | `bool`              | `true`   | Applies defaults to eligible fields without explicit `@cacheControl`. |

# How Effective Response Policy Is Computed

- Only **query operations** participate.
- `max-age` resolves to the most restrictive selected value.
- `s-maxage` resolves to the most restrictive selected shared value.
- Scope resolves to the most restrictive value (`private` over `public`).
- `Vary` values are merged across selected fields.

No cache-control header is emitted when:

- The operation is a mutation.
- The operation result contains errors.
- No selected field contributes cache constraints.

# Skip Cache Control for a Specific Request

Use `SkipQueryCaching()` on `OperationRequestBuilder` to bypass cache-control header generation for a specific request.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

public sealed class NoCacheHeaderInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Skip-Cache-Control"))
        {
            requestBuilder.SkipQueryCaching();
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

Register the interceptor:

```csharp
builder
    .AddGraphQL()
    .AddHttpRequestInterceptor<NoCacheHeaderInterceptor>();
```

# Troubleshooting

## `Cache-Control` header is missing

Cause:

- `.UseQueryCache()` or `.AddCacheControl()` is not configured.
- Selected fields do not contribute cache constraints.
- The response contains GraphQL errors.

Solution:

- Register both middleware and schema support.
- Verify selected fields use `@cacheControl` or enable defaults.
- Check operation errors before validating cache headers.

## Mutations do not include cache headers

Cause:

- Cache-control headers are computed for query operations.

Solution:

- Treat mutation responses as non-cacheable.

## CDN caches private user data

Cause:

- Scope is `public` for user-specific response data.

Solution:

- Use `Scope = CacheControlScope.Private` and relevant `Vary` headers.

# Next Steps

- [Transports](/docs/hotchocolate/v16/server/http-transport) for HTTP transport behavior.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for request-level control.
- [Configuration Options](/docs/hotchocolate/v16/api-reference/options) for full option reference.
