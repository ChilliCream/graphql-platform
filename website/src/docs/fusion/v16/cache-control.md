---
title: "Cache Control"
description: "Understand HTTP Cache-Control and Vary headers, then learn how Fusion uses GraphQL @cacheControl directives to generate safe CDN and browser caching policies."
---

Cache-Control is the HTTP header field that tells browsers, reverse proxies, and CDNs how they are allowed to store and reuse a response instead of sending the same request back to the server every time. Together with related headers such as `Vary`, it makes cached responses safe and predictable by defining whether a response may be reused, how long it may be reused, and which parts of the request affect that decision.

# Why Cache Control Matters

Good cache rules help you:

- Reduce latency for users.
- Reduce load on your backend services.
- Keep cache behavior predictable.
- Protect user-specific data from shared caches.

To make caching work safely and predictably, you need two things:

1. A deterministic GET route so caches get a stable cache key.
2. Correct Cache-Control (and Vary) headers so caches know where and how long a response may be reused.

# Why GraphQL Needs Extra Care

GraphQL usually exposes one endpoint, but each request can ask for different fields. Two requests to the same URL can therefore return very different response shapes and different data sensitivity.

A single GraphQL response can also mix public data and user-specific data. Since HTTP cache headers apply to the full response, the gateway has to compute one safe final policy that represents everything selected in that operation.

That means one response can include:

- Public data (safe for shared caches), and
- User-specific data (not safe for shared caches).

The GraphQL operation type matters. Query operations are side-effect free reads, so they are the primary target for HTTP and CDN caching. Mutations change data and should not be cached as shared HTTP responses. Subscriptions are long-running streams and are not HTTP-cacheable.

# Deterministic GET Routes

The GraphQL over HTTP specification allows query operations to be sent over HTTP GET.

```http
GET /graphql?query=query GetProducts{products{nodes{name}}}
```

The same applies when variables are included.

```http
GET /graphql?query=query GetProducts($first:Int!){products(first:$first){nodes{name}}}&variables={"first":5}
```

In real requests, these values are URL-encoded, and for larger operations the query string quickly becomes difficult to work with. This is where persisted operations help.

## Persisted Operation Routes

In large first-party GraphQL APIs, a common approach used by companies such as Netflix, Meta, and X is to rely on trusted documents. Client operations are stored in an operation store, and clients send a stable operation identifier instead of the full query text.

> You can read more in the [First-Party API guide](/docs/fusion/v16/guides/first-party-api).

With trusted documents in place, persisted-operation routes become short and stable.

```http
GET /graphql/persisted/GetProducts/123456789
```

Variables can then be sent as query parameters.

```http
GET /graphql/persisted/GetProducts/123456789?first=5
```

In order to use persisted-operation routes you need to add the middleware `MapGraphQLPersistedOperations`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far");

var app = builder.Build();

app.MapGraphQLPersistedOperations();
```

# `@cacheControl` in GraphQL

A deterministic route alone is not enough. The gateway also needs policy metadata to decide whether a response is public or private, and how long it may be reused.

GraphQL provides the `@cacheControl` directive for this purpose. You can place it on fields and types to describe cache intent.

```graphql
type Query {
  productById(id: ID!): Product @cacheControl(maxAge: 300, sharedMaxAge: 900)

  me: UserProfile
    @cacheControl(maxAge: 60, scope: PRIVATE, vary: ["Authorization"])
}
```

# How Fusion Assembles the Final Headers

Fusion computes one effective response policy by traversing the planned operation tree. It starts at the selected root fields, reads `@cacheControl` metadata on each field, falls back to the field return type when values are missing, and continues recursively through child selections, including interfaces and unions.

All collected constraints are merged into one final policy. The merge is conservative: `max-age` and `s-maxage` take the lowest value, scope resolves to the strictest value (`private` over `public`), and `vary` values are merged, normalized, and deduplicated.

Fusion computes these cache constraints for query operations. Mutation requests, Subscription request, introspection requests, and operations with no cache constraints do not get cache-control headers.

# Cache Policy Starts in Subgraphs

In Fusion, cache policy starts where data is resolved, which means your subgraphs define cache intent for their own fields.

```graphql
type Product {
  id: ID!
  name: String!
}

type UserProfile {
  id: ID!
  email: String!
}

type Query {
  productById(id: ID!): Product @cacheControl(maxAge: 300, sharedMaxAge: 900)

  me: UserProfile
    @cacheControl(maxAge: 60, scope: PRIVATE, vary: ["Authorization"])
}
```

If your subgraph runs on Hot Chocolate, you can express the same policy with `[CacheControl]` attributes.

```csharp
using HotChocolate.Caching;

[QueryType]
public static class Query
{
    [CacheControl(300, SharedMaxAge = 900)]
    public static Product? GetProductById(int id)
        => ProductRepository.GetById(id);

    [CacheControl(60, Scope = CacheControlScope.Private, Vary = ["Authorization"])]
    public static UserProfile GetMe()
        => UserProfileRepository.GetCurrent();
}
```

Enable cache-control metadata on the subgraph:

```csharp
builder
    .AddGraphQL()
    .AddCacheControl();
```

# Enable Cache Control in the Fusion Gateway

The gateway must be configured to read cache-control metadata during planning and to write the final HTTP headers on the response.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .AddCacheControl()
    .UseQueryCache();
```

`AddCacheControl()` enables cache-constraint planning. `UseQueryCache()` writes the final `Cache-Control` and `Vary` headers to HTTP responses.

# Putting It Together

In day-to-day terms, the flow is simple:

- Subgraphs declare cache intent.
- Fusion composes that metadata.
- The gateway calculates one safe policy for each query result.
- HTTP caches enforce the resulting headers.
