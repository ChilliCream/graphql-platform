---
title: Cache control
---

Cache control in Hot Chocolate allows you to specify how a GraphQL query response can be reused by HTTP caches. In version 16, Hot Chocolate reads cache metadata from the selected fields and types, determines a conservative policy for the entire response, and emits HTTP `Cache-Control` and `Vary` headers accordingly.

Hot Chocolate does not store GraphQL responses, operate a CDN, purge external caches, or invalidate cached objects. For cache control to improve latency or reduce server load, a browser, reverse proxy, CDN, or another HTTP cache must receive the response and honor the headers.

# When to Use Cache Control

Cache control is beneficial for query responses that are safe to reuse, such as:

- Public catalog, product, lookup, reference, or mostly static data
- Expensive query fields that can be reused for a limited time
- First-party clients using deterministic GET requests, trusted documents, persisted operations, or APQ hashes
- APIs behind browsers, reverse proxies, or CDNs that respect `Cache-Control` and `Vary`

Cache control is not suitable for:

- Mutations and subscriptions
- Introspection responses
- Highly personalized, role-filtered, tenant-filtered, or locale-specific data unless the response is private or the varying request headers are part of the cache key
- Deployments without an HTTP cache in front of the server or when clients never reuse responses

# GraphQL and HTTP Caching

HTTP cache headers apply to the entire response. Since a single GraphQL operation can select both public and private data, Hot Chocolate merges all selected cache hints into a single, safe response policy.

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

If `productById` is public for 300 seconds and `me` is private for 60 seconds, the final response must be private and use the shorter age. A single private field constrains the entire response.

# Use a Stable Request Shape

Most shared HTTP caches are designed around GET cache keys. Hot Chocolate can emit cache headers for query responses regardless of whether the request used GET or POST, but GET requests and persisted operation routes are more practical for reuse by browsers, proxies, and CDNs.

A direct GET request can include the operation and variables:

```http
GET /graphql?query=query%20GetProduct($id:ID!){productById(id:$id){id%20name}}&variables={"id":"1"}
```

A persisted operation route can use a stable operation identifier and variables:

```http
GET /graphql/persisted/4f3ad2/GetProduct?variables={"id":"1"}
```

Persisted operations, trusted documents, and APQ help create stable operation identifiers. However, these alone do not make a response safe for public caching. The selected fields must still have correct cache metadata.

For more on request shapes, see [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport), [endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints), [trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents), and [automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations).

# Understand Your Target Cache

| Cache                                    | Owner                           | Typical storage                          | Relevant headers                | Warning                                                                                        |
| ---------------------------------------- | ------------------------------- | ---------------------------------------- | ------------------------------- | ---------------------------------------------------------------------------------------------- |
| Browser or private client cache          | One user agent                  | One user's responses                     | `max-age`, `private`            | Private responses can still be stored by that user's browser.                                  |
| Reverse proxy                            | Your infrastructure             | Shared responses for many users          | `s-maxage`, `Vary`, `private`   | Auth, tenant, role, and locale boundaries must be in the cache policy and proxy configuration. |
| CDN                                      | External or edge infrastructure | Shared edge responses                    | `s-maxage`, `Vary`              | Plan cache keys, purge, and revalidation outside Hot Chocolate.                                |
| Relay or another normalized client store | Client application              | Normalized GraphQL records               | Not controlled by these headers | This is client state, not Hot Chocolate cache control.                                         |
| DataLoader cache                         | Hot Chocolate request execution | One GraphQL request                      | Not controlled by these headers | Use it to batch and deduplicate backend loads within a request.                                |
| Application cache                        | Your resolver or service layer  | Memory, Redis, database, or domain cache | Usually not HTTP headers        | Use it when you need server-side reuse or invalidation rules.                                  |

# Enabling Cache Control

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

`UseQueryCache()` adds request middleware that transfers computed constraints into the result context so the HTTP response formatter can emit `Cache-Control` and `Vary`. This middleware does not store responses. By default, it is inserted after the timeout middleware.

For authenticated APIs, it is recommended to start with `ApplyDefaults = false` and explicitly opt in known public fields.

# Adding Cache Metadata

Hot Chocolate v16 allows you to specify cache metadata using attribute-based schemas, fluent code-first configuration, or schema-first SDL.

## Attribute-Based Schema

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

If `productById` is selected by itself, the HTTP response will include:

```http
Cache-Control: max-age=300, s-maxage=900
```

If `me` is selected, the response is private. The `Vary: Authorization` header tells an HTTP cache that the `Authorization` request header changes the representation. However, this is not a universal safety mechanism for shared caches. Avoid shared storage for authenticated responses unless your proxy or CDN is specifically configured and tested for this scenario.

## Code-First Fluent Configuration

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

Generic object and interface descriptor overloads provide `maxAge` and `scope`.

## Schema-First SDL

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

The `@cacheControl` directive can be applied to object types, field definitions, interface types, and union types. Hot Chocolate v16 will reject cache control applied to interface fields during schema validation.

# Argument Reference

| Concept                           | Attribute or fluent API                                   | SDL argument          | HTTP effect                                    | Notes                                                                                              |
| --------------------------------- | --------------------------------------------------------- | --------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Browser or private-cache lifetime | `MaxAge`, `maxAge`                                        | `maxAge`              | `Cache-Control: max-age=<seconds>`             | Must be non-negative. A value of `0` is a valid policy.                                            |
| Shared-cache lifetime             | `SharedMaxAge`, `sharedMaxAge`                            | `sharedMaxAge`        | `Cache-Control: s-maxage=<seconds>`            | Must be non-negative. Use for proxies and CDNs.                                                    |
| Scope                             | `CacheControlScope.Public` or `CacheControlScope.Private` | `PUBLIC` or `PRIVATE` | `private` when any selected field is private   | Private is the stricter scope.                                                                     |
| Inherit parent age                | `InheritMaxAge`, `inheritMaxAge`                          | `inheritMaxAge`       | Keeps the current parent age for a child field | Valid only on non-root object fields. Cannot be combined with explicit `maxAge` or `sharedMaxAge`. |
| Vary headers                      | `Vary`, `vary`                                            | `vary`                | `Vary: <headers>`                              | Values are lowercased, sorted, and deduplicated in the response.                                   |

The package and namespace are `HotChocolate.Caching`. The scope enum is `CacheControlScope`.

# How Hot Chocolate Computes Headers

Hot Chocolate computes cache constraints for query operations only:

1. Start at the selected root query fields.
2. Read `@cacheControl` metadata on each field.
3. If a field does not provide every value, read metadata from the field's return type if it is a complex type.
4. Walk through child selections, including possible object types behind interfaces and unions.
5. Merge all collected constraints into a single response policy.

Introspection requests do not receive cache-control headers, except when `__typename` appears inside a normal selection. This does not make the entire request introspection.

## Merge Rules

| Constraint                | Merge rule                                                                                                                                                                                      |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `max-age`                 | The lowest selected `maxAge` is used.                                                                                                                                                           |
| `s-maxage`                | The lowest selected `sharedMaxAge` is used.                                                                                                                                                     |
| `max-age` with `s-maxage` | If a selected `maxAge` is lower than an existing shared age and no explicit `sharedMaxAge` overrides it, the shared age is reduced so a shared cache does not outlive that field's browser age. |
| Scope                     | `PRIVATE` takes precedence over `PUBLIC`.                                                                                                                                                       |
| `Vary`                    | Header names are merged case-insensitively, lowercased, sorted, and deduplicated.                                                                                                               |
| No age                    | If no selected field or fallback type contributes `maxAge` or `sharedMaxAge`, no cache policy is emitted.                                                                                       |

## Header Examples

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

# Configuring Defaults

You can configure global cache-control behavior using `ModifyCacheControlOptions(...)`:

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

Defaults are applied when both `Enable` and `ApplyDefaults` are true. They affect query type fields and data resolver fields that do not already have cache metadata. Defaults are not applied to introspection, mutation, or subscription types.

With the built-in defaults, eligible fields can contribute `max-age=0`. For authenticated or multi-tenant APIs, it is safer to use an opt-in approach:

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

# Dynamic and Request-Specific Behavior

In Hot Chocolate v16, cache hints are schema metadata. If a field can return public data in some cases and private data in others, it is best to use a conservative static policy such as `PRIVATE`, split the schema into separate fields with different policies, or avoid shared HTTP caches for that response.

For request-level decisions, you can use an HTTP request interceptor to opt out of cache-control header generation by calling `SkipQueryCaching()` on the `OperationRequestBuilder`:

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

Skipping query caching prevents Hot Chocolate from adding cache-control headers for that request. It does not purge external caches or bypass a CDN that already has a matching stored response.

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

# What Cache Control Is Not

Cache control does not replace other performance features:

- DataLoader batches and caches backend loads within a single GraphQL request
- The operation document cache stores parsed and validated documents per schema
- The prepared operation cache stores prepared operation work per schema
- Persisted operation storage and APQ storage map IDs or hashes to operation documents
- Trusted documents enforce an allow list of known operations when configured as a security policy
- Application-level caches such as `IMemoryCache`, Redis, database caches, and domain caches store data or computed results on the server side

Use application-level caching or DataLoader when you want to avoid repeated resolver data access, database calls, service calls, or key loading within a request. Use HTTP cache control only when the entire GraphQL query response is safe for reuse by an HTTP cache.

# Troubleshooting

## Cache-Control Header Is Missing

If you do not see a `Cache-Control` header, check the following:

- Is `HotChocolate.Caching` installed?
- Did you register both `.UseQueryCache()` and `.AddCacheControl()` in your schema?
- Is `CacheControlOptions.Enable` set to true?
- Is the operation a query?
- Is the request an introspection request?
- Did the result contain GraphQL errors?
- Did any selected field or fallback type contribute `maxAge` or `sharedMaxAge`?
- Is `ApplyDefaults` set to false with no explicit cache metadata?
- Did an interceptor call `SkipQueryCaching()`?
- Did downstream ASP.NET Core middleware, hosting, a reverse proxy, or a CDN strip the headers?

## CDN Caches Private Data

If your CDN is caching private data, consider these causes:

- Authenticated, tenant-specific, role-specific, or locale-specific fields are missing `CacheControlScope.Private`
- A public field returns data that depends on `Authorization`, `Cookie`, tenant headers, role headers, or locale headers
- Required request headers are missing from `Vary` or are ignored by the proxy or CDN cache key configuration
- The CDN is configured to cache authenticated responses despite `private` or authorization headers

Avoid shared caching for authenticated routes unless the full request path, selected fields, headers, CDN cache key, and purge behavior are designed and tested together.

## Low Cache Hit Rate

Common reasons for a low cache hit rate include:

- Too many `Vary` values split the cache key
- Variables create many distinct URLs or persisted-operation keys
- POST requests are not cached by your infrastructure
- Short `maxAge` values cause responses to expire quickly
- Mixed private fields make the entire response private
- Ad-hoc query text changes the cache key; consider trusted documents, persisted operations, or APQ for stable identifiers

## Schema Validation Fails

Hot Chocolate v16 validates cache-control metadata at schema build time. Common causes for validation failures include:

- Negative `maxAge` or `sharedMaxAge`
- `inheritMaxAge` on an object, interface, or union type
- `inheritMaxAge` on a root query field
- `inheritMaxAge` combined with explicit `maxAge` or `sharedMaxAge` on the same field
- Cache control applied to an interface field

# Next Steps

- [HTTP transport](/docs/hotchocolate/v16/build/server-configuration/http-transport): Learn about GET, POST, batching, and response behavior
- [Endpoints](/docs/hotchocolate/v16/build/server-configuration/endpoints): Explore persisted operation routes and GET settings
- [Automatic persisted operations](/docs/hotchocolate/v16/build/performance/automatic-persisted-operations): Use APQ and hash-only requests
- [Trusted documents](/docs/hotchocolate/v16/build/security/trusted-documents): Enforce allow-listed operation security
- [DataLoader](/docs/hotchocolate/v16/build/dataloader): Implement request-scoped batching and caching
- [Performance](/docs/hotchocolate/v16/build/performance): Review the broader performance workflow
