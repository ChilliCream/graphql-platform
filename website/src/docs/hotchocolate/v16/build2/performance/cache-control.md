---
title: Cache control
---

Cache control lets Hot Chocolate describe how a GraphQL query response may be reused by HTTP caches. Hot Chocolate v16 reads cache metadata from the selected fields and types, computes one conservative policy for the whole response, and emits HTTP `Cache-Control` and `Vary` headers.

It does not store GraphQL responses, run a CDN, purge external caches, or invalidate cached objects. A browser, reverse proxy, CDN, or another HTTP cache must receive the response and honor the headers before cache control improves latency or server load.

# When cache control helps

Use cache control for query responses that are safe to reuse:

- Public catalog, product, lookup, reference, or mostly static data.
- Expensive query fields that can be reused for a bounded time.
- First-party clients that use deterministic GET requests, trusted documents, persisted operations, or APQ hashes.
- APIs behind browsers, reverse proxies, or CDNs that respect `Cache-Control` and `Vary`.

Cache control is a poor fit for:

- Mutations and subscriptions.
- Introspection responses.
- Highly personalized, role-filtered, tenant-filtered, or locale-specific data unless the response is private or the varying request headers are part of the cache key.
- Deployments without an HTTP cache in front of the server or client behavior that never reuses responses.

# GraphQL and HTTP caching

HTTP cache headers apply to the whole response. GraphQL lets a single operation select public data and private data at the same time, so Hot Chocolate must merge all selected cache hints into one safe response policy.

```graphql
query ProductAndViewer($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
  me {
    name
  }
}
```

If `productById` is public for 300 seconds and `me` is private for 60 seconds, the final response must be private and use the shorter age. One private selected field constrains the whole response.

# Use a stable request shape

Most shared HTTP caches are designed around GET cache keys. Hot Chocolate can emit cache headers for executed query responses regardless of whether the request used GET or POST, but GET requests and persisted operation routes are more practical for browsers, proxies, and CDNs to reuse.

A direct GET request can contain the operation and variables:

```http
GET /graphql?query=query%20GetProduct($id:ID!){productById(id:$id){id%20name}}&variables={"id":"1"}
```

A persisted operation route can use a stable operation identifier and variables:

```http
GET /graphql/persisted/4f3ad2/GetProduct?variables={"id":"1"}
```

Persisted operations, trusted documents, and APQ help produce stable operation identifiers. They do not make a response public-cache safe by themselves. The selected fields still need correct cache metadata.

Read more about request shapes in [HTTP transport](/docs/hotchocolate/v16/build2/server-configuration/http-transport), [endpoints](/docs/hotchocolate/v16/server/endpoints), [trusted documents](/docs/hotchocolate/v16/build2/security/trusted-documents), and [automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations).

# Know which cache you are targeting

| Cache                                    | Owner                           | Typical storage                          | Relevant headers                | Warning                                                                                        |
| ---------------------------------------- | ------------------------------- | ---------------------------------------- | ------------------------------- | ---------------------------------------------------------------------------------------------- |
| Browser or private client cache          | One user agent                  | One user's responses                     | `max-age`, `private`            | Private responses can still be stored by that user's browser.                                  |
| Reverse proxy                            | Your infrastructure             | Shared responses for many users          | `s-maxage`, `Vary`, `private`   | Auth, tenant, role, and locale boundaries must be in the cache policy and proxy configuration. |
| CDN                                      | External or edge infrastructure | Shared edge responses                    | `s-maxage`, `Vary`              | Plan cache keys, purge, and revalidation outside Hot Chocolate.                                |
| Relay or another normalized client store | Client application              | Normalized GraphQL records               | Not controlled by these headers | This is client state, not Hot Chocolate cache control.                                         |
| DataLoader cache                         | Hot Chocolate request execution | One GraphQL request                      | Not controlled by these headers | Use it to batch and deduplicate backend loads within a request.                                |
| Application cache                        | Your resolver or service layer  | Memory, Redis, database, or domain cache | Usually not HTTP headers        | Use it when you need server-side reuse or invalidation rules.                                  |

# Enable cache control

Install the package:

<PackageInstallation packageName="HotChocolate.Caching" />

Register the query cache middleware and cache-control services:

```csharp
using HotChocolate.Caching;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(options =>
    {
        options.ApplyDefaults = false;
    });
```

`AddCacheControl()` registers the `@cacheControl` directive, schema validation, default application, and operation optimization needed to compute cache constraints.

`UseQueryCache()` adds request middleware that transfers computed constraints into the result context so the HTTP response formatter can emit `Cache-Control` and `Vary`. The middleware does not store responses. By default, it is inserted after the timeout middleware.

For existing authenticated APIs, start with `ApplyDefaults = false` and opt in known public fields first.

# Add cache metadata

Hot Chocolate v16 supports cache metadata in attribute-based schemas, fluent code-first configuration, and schema-first SDL.

## Attribute-based schema

```csharp
using HotChocolate.Authorization;
using HotChocolate.Caching;

[QueryType]
public static class ProductQueries
{
    [CacheControl(300, SharedMaxAge = 900)]
    public static Product? GetProductById(string id)
        => ProductRepository.GetById(id);

    [Authorize]
    [CacheControl(
        60,
        Scope = CacheControlScope.Private,
        Vary = ["Authorization"])]
    public static UserProfile GetMe(IUserContext user)
        => UserProfileRepository.GetByUserId(user.Id);
}
```

When `productById` is selected alone, the expected HTTP policy is:

```http
Cache-Control: max-age=300, s-maxage=900
```

When `me` is selected, the response is private. `Vary: Authorization` tells an HTTP cache that the `Authorization` request header changes the representation, but it is not a universal safety mechanism for shared caches. Prefer no shared storage for authenticated responses unless your proxy or CDN is deliberately configured and tested for that pattern.

## Code-first fluent configuration

Field descriptors support all cache-control arguments:

```csharp
using HotChocolate.Caching;
using HotChocolate.Types;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("productById")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Resolve(context => ProductRepository.GetById(
                context.ArgumentValue<string>("id")))
            .CacheControl(
                maxAge: 300,
                scope: CacheControlScope.Public,
                sharedMaxAge: 900);
    }
}
```

Non-generic object, interface, and union type descriptors support type-level `maxAge`, `scope`, `sharedMaxAge`, and `vary`:

```csharp
using HotChocolate.Caching;
using HotChocolate.Types;

public sealed class ProductType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .CacheControl(
                maxAge: 300,
                scope: CacheControlScope.Public,
                sharedMaxAge: 900,
                vary: ["Accept-Language"]);

        descriptor.Field("id").Type<NonNullType<IdType>>();
        descriptor.Field("name");
    }
}
```

Generic object and interface descriptor overloads expose `maxAge` and `scope`.

## Schema-first SDL

```graphql
type Product @cacheControl(maxAge: 300, scope: PUBLIC) {
  id: ID!
  name: String!
  price: Decimal! @cacheControl(maxAge: 60)
}

type Query {
  productById(id: ID!): Product @cacheControl(sharedMaxAge: 900)

  me: UserProfile
    @cacheControl(maxAge: 60, scope: PRIVATE, vary: ["Authorization"])
}
```

The directive can be applied to object types, field definitions, interface types, and union types. Hot Chocolate v16 validation rejects cache control applied to interface fields.

# Argument reference

| Concept                           | Attribute or fluent API                                   | SDL argument          | HTTP effect                                    | Notes                                                                                              |
| --------------------------------- | --------------------------------------------------------- | --------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Browser or private-cache lifetime | `MaxAge`, `maxAge`                                        | `maxAge`              | `Cache-Control: max-age=<seconds>`             | Must be non-negative. A value of `0` is a valid policy.                                            |
| Shared-cache lifetime             | `SharedMaxAge`, `sharedMaxAge`                            | `sharedMaxAge`        | `Cache-Control: s-maxage=<seconds>`            | Must be non-negative. Use for proxies and CDNs.                                                    |
| Scope                             | `CacheControlScope.Public` or `CacheControlScope.Private` | `PUBLIC` or `PRIVATE` | `private` when any selected field is private   | Private is the stricter scope.                                                                     |
| Inherit parent age                | `InheritMaxAge`, `inheritMaxAge`                          | `inheritMaxAge`       | Keeps the current parent age for a child field | Valid only on non-root object fields. Cannot be combined with explicit `maxAge` or `sharedMaxAge`. |
| Vary headers                      | `Vary`, `vary`                                            | `vary`                | `Vary: <headers>`                              | Values are lowercased, sorted, and deduplicated in the response.                                   |

Package and namespace: `HotChocolate.Caching`. Scope enum: `CacheControlScope`.

# How Hot Chocolate computes headers

Hot Chocolate computes constraints for query operations only:

1. Start at selected root query fields.
2. Read `@cacheControl` metadata on a field.
3. If the field did not provide every value, read metadata from the field return type when it is a complex type.
4. Walk child selections, including possible object types behind interfaces and unions.
5. Merge all collected constraints into one response policy.

Introspection requests do not receive cache-control headers, except that `__typename` can appear inside a normal selection without making the whole request introspection.

## Merge rules

| Constraint                | Merge rule                                                                                                                                                                                      |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `max-age`                 | Lowest selected `maxAge` wins.                                                                                                                                                                  |
| `s-maxage`                | Lowest selected `sharedMaxAge` wins.                                                                                                                                                            |
| `max-age` with `s-maxage` | If a selected `maxAge` is lower than an existing shared age and no explicit `sharedMaxAge` overrides it, the shared age is reduced so a shared cache does not outlive that field's browser age. |
| Scope                     | `PRIVATE` wins over `PUBLIC`.                                                                                                                                                                   |
| `Vary`                    | Header names are merged case-insensitively, lowercased, sorted, and deduplicated.                                                                                                               |
| No age                    | If no selected field or fallback type contributes `maxAge` or `sharedMaxAge`, no cache policy is emitted.                                                                                       |

## Header examples

| Selected metadata                                                     | Result headers                          |
| --------------------------------------------------------------------- | --------------------------------------- |
| One field with `maxAge: 2000`                                         | `Cache-Control: max-age=2000`           |
| One field with `maxAge: 0`                                            | `Cache-Control: max-age=0`              |
| Public field with `maxAge: 30` and private field with `maxAge: 60`    | `Cache-Control: max-age=30, private`    |
| Private field with `sharedMaxAge: 30`                                 | `Cache-Control: s-maxage=30, private`   |
| Field with `maxAge: 0, sharedMaxAge: 60` plus field with `maxAge: 30` | `Cache-Control: max-age=0, s-maxage=30` |
| Fields with `vary: ["X-foo", "X-BaR"]` and `vary: ["X-FAR", "X-BaR"]` | `Vary: x-bar, x-far, x-foo`             |
| `ApplyDefaults = false` and no explicit ages                          | No `Cache-Control` header               |
| Executed result contains GraphQL errors                               | No cache-control headers                |

# Configure defaults

`ModifyCacheControlOptions(...)` configures global behavior:

```csharp
using HotChocolate.Caching;

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(options =>
    {
        options.Enable = true;
        options.DefaultMaxAge = 60;
        options.DefaultScope = CacheControlScope.Public;
        options.ApplyDefaults = true;
    });
```

| Option          | Type                | Default  | Description                                                          |
| --------------- | ------------------- | -------- | -------------------------------------------------------------------- |
| `Enable`        | `bool`              | `true`   | Enables cache-control processing and header generation.              |
| `DefaultMaxAge` | `int`               | `0`      | Default `maxAge` for eligible fields.                                |
| `DefaultScope`  | `CacheControlScope` | `Public` | Default scope for eligible fields.                                   |
| `ApplyDefaults` | `bool`              | `true`   | Applies defaults to eligible fields without explicit cache metadata. |

Defaults apply when `Enable` and `ApplyDefaults` are both true. They are applied to query type fields and data resolver fields that do not already have cache metadata. Defaults are skipped for introspection, mutation, and subscription types.

With the built-in defaults, eligible fields can contribute `max-age=0`. For authenticated or multi-tenant APIs, prefer an opt-in rollout:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(options =>
    {
        options.ApplyDefaults = false;
    });
```

# Dynamic and request-specific behavior

Hot Chocolate v16 cache hints are schema metadata. If a field can return public data in one case and private data in another, prefer a conservative static policy such as `PRIVATE`, split the schema into separate fields with different policies, or keep the response out of shared HTTP caches.

For request-level decisions, an HTTP request interceptor can opt out of cache-control header generation by calling `SkipQueryCaching()` on the `OperationRequestBuilder`:

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Caching;
using HotChocolate.Execution;

public sealed class CachePolicyInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Skip-GraphQL-Cache-Control"))
        {
            requestBuilder.SkipQueryCaching();
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Register the interceptor:

```csharp
builder
    .AddGraphQL()
    .AddHttpRequestInterceptor<CachePolicyInterceptor>();
```

Skipping query caching prevents Hot Chocolate from adding cache-control headers for that request. It does not purge external caches and it does not bypass a CDN that already has a matching stored response.

# Safe patterns

## Public catalog data through a CDN

```csharp
using HotChocolate.Caching;

[QueryType]
public static class ProductQueries
{
    [CacheControl(300, SharedMaxAge = 900)]
    public static Product? GetProductById(string id)
        => ProductRepository.GetById(id);
}
```

A persisted GET request gives the CDN a stable key:

```http
GET /graphql/persisted/4f3ad2/GetProduct?variables={"id":"1"}
```

The response can be reused by a browser for 300 seconds and by a shared cache for 900 seconds, unless another selected field contributes a stricter policy:

```http
Cache-Control: max-age=300, s-maxage=900
```

Plan purge, revalidation, surrogate keys, or cache tags in your CDN or application layer. Hot Chocolate does not manage those concerns.

## User profile data

```csharp
using HotChocolate.Authorization;
using HotChocolate.Caching;

[QueryType]
public static class ViewerQueries
{
    [Authorize]
    [CacheControl(60, Scope = CacheControlScope.Private)]
    public static UserProfile GetMe(IUserContext user)
        => UserProfileRepository.GetByUserId(user.Id);
}
```

This allows a private cache, such as the user's browser, to reuse the response for a short time. It marks the final response private when selected with public data.

## Mixed public and private selection

```graphql
query ProductAndViewer($id: ID!) {
  productById(id: $id) {
    id
    name
  }
  me {
    name
  }
}
```

If `productById` has `maxAge: 300, sharedMaxAge: 900` and `me` has `maxAge: 60, scope: PRIVATE`, the final response is constrained by the private field:

```http
Cache-Control: max-age=60, private
```

# What cache control is not

Cache control is not a replacement for other performance features:

- DataLoader batches and caches backend loads within one GraphQL request.
- The operation document cache stores parsed and validated documents per schema.
- The prepared operation cache stores prepared operation work per schema.
- Persisted operation storage and APQ storage map IDs or hashes to operation documents.
- Trusted documents enforce an allow list of known operations when configured as a security policy.
- Application-level caches such as `IMemoryCache`, Redis, database caches, and domain caches store data or computed results on the server side.

Use application-level caching or DataLoader when the work you want to avoid is resolver data access, database calls, service calls, or repeated key loading within a request. Use HTTP cache control when the entire GraphQL query response is safe for an HTTP cache to reuse.

# Troubleshooting

## I do not see a Cache-Control header

Check these causes:

- Is `HotChocolate.Caching` installed?
- Did the schema registration call both `.UseQueryCache()` and `.AddCacheControl()`?
- Is `CacheControlOptions.Enable` true?
- Is the operation a query?
- Is the request an introspection request?
- Did the result contain GraphQL errors?
- Did any selected field or fallback type contribute `maxAge` or `sharedMaxAge`?
- Is `ApplyDefaults` false with no explicit cache metadata?
- Did an interceptor call `SkipQueryCaching()`?
- Did downstream ASP.NET Core middleware, hosting, a reverse proxy, or a CDN strip the headers?

## My CDN caches private data

Check these causes:

- Authenticated, tenant-specific, role-specific, or locale-specific fields are missing `CacheControlScope.Private`.
- A public field returns data that depends on `Authorization`, `Cookie`, tenant headers, role headers, or locale headers.
- Required request headers are missing from `Vary` or are ignored by the proxy or CDN cache key configuration.
- The CDN is configured to cache authenticated responses despite `private` or authorization headers.

Prefer no shared caching for authenticated routes unless the full request path, selected fields, headers, CDN cache key, and purge behavior are designed and tested together.

## My cache hit rate is low

Common causes:

- Too many `Vary` values split the cache key.
- Variables create many distinct URLs or persisted-operation keys.
- POST requests are not cached by the chosen infrastructure.
- Short `maxAge` values expire responses quickly.
- Mixed private fields make the whole response private.
- Ad-hoc query text changes the cache key. Consider trusted documents, persisted operations, or APQ for stable identifiers.

## Schema validation fails

Hot Chocolate v16 validates cache-control metadata at schema build time. Common causes include:

- Negative `maxAge` or `sharedMaxAge`.
- `inheritMaxAge` on an object, interface, or union type.
- `inheritMaxAge` on a root query field.
- `inheritMaxAge` combined with explicit `maxAge` or `sharedMaxAge` on the same field.
- Cache control applied to an interface field.

# Next steps

- [HTTP transport](/docs/hotchocolate/v16/build2/server-configuration/http-transport) for GET, POST, batching, and response behavior.
- [Endpoints](/docs/hotchocolate/v16/server/endpoints) for persisted operation routes and GET settings.
- [Automatic persisted operations](/docs/hotchocolate/v16/build2/performance/automatic-persisted-operations) for APQ and hash-only requests.
- [Trusted documents](/docs/hotchocolate/v16/build2/security/trusted-documents) for allow-listed operation security.
- [DataLoader](/docs/hotchocolate/v16/build2/dataloader) for request-scoped batching and caching.
- [Performance](/docs/hotchocolate/v16/build2/performance) for the broader performance workflow.
