---
title: "Caching for performance"
---

Caching can significantly improve the performance of a Hot Chocolate server, but every cache must have a well-defined boundary. Before adding any cache, ask yourself:

1. What data will be cached?
2. Who can reuse this cached data?
3. When and how does the cache expire or get invalidated?

Hot Chocolate v16 provides several caching mechanisms: request-scoped DataLoader caching, parsed document caches, prepared operation caches, persisted operation storage, and cache-control metadata for HTTP responses. However, it does not include a built-in response-body cache. Full response reuse is handled by browsers, reverse proxies, CDNs, or application-level caches that you configure around your GraphQL server.

# Prerequisites

This guide assumes you are running a Hot Chocolate v16 ASP.NET Core server and are familiar with queries, resolvers, and basic hosting concepts.

The examples here focus on query operations. Mutations change server state and should not be cached as shared HTTP responses. Subscriptions are long-running streams and are not HTTP-cacheable in the same way.

Fusion gateway and subgraph cache composition are not covered on this page.

Begin with a standard server setup:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();
```

Add the `HotChocolate.Caching` package only if you want Hot Chocolate to compute cache metadata and emit HTTP `Cache-Control` and `Vary` headers:

```bash
dotnet add package HotChocolate.Caching
```

# Choose the Right Cache Layer

Don’t add every cache at once. Select the layer that matches the specific work you want to avoid.

| Problem                                        | Layer                             | Scope                     | Owner                             | Invalidates when                                                       | Watch out                                                                           |
| ---------------------------------------------- | --------------------------------- | ------------------------- | --------------------------------- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| Duplicate key loads inside one GraphQL request | DataLoader cache                  | One request               | Hot Chocolate and GreenDonut      | Request ends                                                           | Does not help the next request.                                                     |
| Repeated parsing of the same GraphQL document  | Document cache                    | One executor instance     | Hot Chocolate                     | Capacity eviction or executor rebuild                                  | Dynamic query text can churn the cache.                                             |
| Repeated compilation of the same operation     | Prepared operation cache          | One executor instance     | Hot Chocolate                     | Capacity eviction or executor rebuild                                  | Stable operation IDs improve reuse.                                                 |
| Sending known operations by ID                 | Persisted operation storage       | Storage backend           | Your deployment and Hot Chocolate | Deployment, versioning, TTL, or storage lifecycle                      | Stores GraphQL documents, not JSON results.                                         |
| Expensive domain data reused across requests   | Resolver or domain cache          | Your application boundary | Your application                  | Writes, events, TTLs, or data-platform invalidation                    | Include authorization, tenant, locale, and arguments in keys when they affect data. |
| Complete public query responses                | HTTP, reverse proxy, or CDN cache | Browser, proxy, or CDN    | Infrastructure                    | TTL, purge, or cache-key change                                        | Highest privacy risk. Headers apply to the whole GraphQL response.                  |
| Records in the client UI                       | Client cache                      | One client application    | Client framework                  | Fetch policy, normalized-store updates, refetch, or garbage collection | Server/CDN invalidation does not update every client store.                         |

A slow product catalog query might require multiple cache layers, but each layer addresses a different concern. Use DataLoader for N+1 fan-out, persisted operations for stable operation identity, domain caches for reusable read models, and HTTP/CDN caching only for complete responses that are safe to reuse.

# Use DataLoader for Repeated Keys Within a Request

DataLoader is the safest first cache for solving GraphQL N+1 problems. It batches key lookups and caches loaded values for the duration of a single operation.

```csharp
using Microsoft.EntityFrameworkCore;

internal static class BrandDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        CatalogContext db,
        CancellationToken ct)
        => await db.Brands
            .Where(b => ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, ct);
}

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

If one operation resolves five products with brand IDs `[1, 2, 1, 3, 2]`, the batch function receives the deduplicated keys:

```text
Requested brand IDs in one operation: [1, 2, 1, 3, 2]
Batch function receives: [1, 2, 3]
```

Each request starts with an empty DataLoader cache. Use a domain cache behind the batch function if the loaded data is expensive and safe to reuse across requests.

Learn the full DataLoader model in [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

# Understand document and prepared operation caches

Hot Chocolate includes in-memory caches in the request pipeline. The default pipeline, trusted-document pipeline, and automatic persisted operation pipeline use the document cache and prepared operation cache.

| Cache                    | What is cached           | Key source                   | Default size | Minimum size |
| ------------------------ | ------------------------ | ---------------------------- | ------------ | ------------ |
| Document cache           | Parsed GraphQL documents | Document ID or document hash | `256`        | `16`         |
| Prepared operation cache | Compiled operation plans | Operation ID                 | `256`        | `16`         |

Configure the capacities with schema options:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ModifyOptions(o =>
    {
        o.OperationDocumentCacheSize = 1024;
        o.PreparedOperationCacheSize = 1024;
    });
```

These caches are per executor instance. They are not distributed, and they do not store response bodies. Increasing their size helps only when repeated documents or operation IDs are being evicted too quickly and the extra memory is acceptable.

High-cardinality dynamic documents reduce hit rate. Persisted operations help because clients send stable operation IDs instead of many query text variants.

The prepared operation cache also coalesces concurrent compilation. When many requests compile the same uncached operation at the same time, one request leads the compilation and followers await the same result.

# Use persisted operations when you need stable operation identity

Persisted operations let clients send an operation `id` instead of a full `query`. This improves performance by reducing payload size and stabilizing document identity. It also supports production security because you can reject dynamic operations.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations")
    .ModifyRequestOptions(o =>
        o.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

A client request sends the operation ID and variables:

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": { "id": "UHJvZHVjdDox" }
}
```

Hot Chocolate resolves the GraphQL document from storage, then executes it like any other operation.

Persisted operation storage is not a result cache. It stores GraphQL documents by ID. Trusted documents are registered before or during deployment. Automatic persisted operations store documents dynamically at runtime after the first miss.

Choose storage by deployment topology:

| Storage            | Use when                                                                   |
| ------------------ | -------------------------------------------------------------------------- |
| File system        | You package operation files with the app or mount them at deployment time. |
| Redis              | Multiple server instances need shared operation storage.                   |
| Azure Blob Storage | You want object storage and storage-level lifecycle management.            |

The hash algorithm and encoding must match the client. Hot Chocolate supports MD5, SHA1, and SHA256 providers, with base64 or hex formatting.

See [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for complete setup guides.

# Use GET and persisted-operation routes for CDN-friendly keys

Most CDNs key on method, host, path, query string, and selected headers. A GraphQL POST body is not usually a good shared-cache key. For public cacheable queries, prefer GET and stable operation IDs.

Configure the normal GraphQL endpoint and the persisted-operation endpoint:

```csharp
var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLPersistedOperations();
```

The default persisted-operation path is `/graphql/persisted`. Hot Chocolate maps these route shapes:

```http
GET /graphql/persisted/{operationId}
GET /graphql/persisted/{operationId}/{operationName}
```

Variables and extensions can be sent as query parameters:

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProduct?variables={"id":"1"}
```

A full query can also be sent over GET, but it is longer and less stable:

```http
GET /graphql?query=query%20GetProduct($id:ID!){product(id:$id){name}}&variables={"id":"1"}
```

Variables still affect the response. If a CDN uses the query string as part of the key, encode variables consistently. If tenant, locale, feature flags, cookies, or authorization headers affect the result, account for them with `Vary`, mark the response private, or bypass shared caching.

See [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for request semantics.

# Emit HTTP cache headers for public query responses

Use `HotChocolate.Caching` when you want Hot Chocolate to compute a response-level cache policy from schema metadata.

```csharp
using HotChocolate.Caching;

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UseQueryCache()
    .AddCacheControl()
    .ModifyCacheControlOptions(o =>
    {
        o.ApplyDefaults = false;
    });

[QueryType]
public static class Query
{
    [CacheControl(300, SharedMaxAge = 900)]
    public static Product? GetProductById(int id)
        => ProductRepository.GetById(id);
}
```

For this query:

```graphql
query {
  productById(id: 1) {
    name
  }
}
```

Hot Chocolate writes response metadata that the ASP.NET Core response formatter emits as HTTP headers:

```http
Cache-Control: max-age=300, s-maxage=900
```

`AddCacheControl()` registers the `@cacheControl` directive and operation compiler support. `UseQueryCache()` registers middleware that writes `Cache-Control` and `Vary` values to the operation result when the executed query succeeds.

:::warning
`UseQueryCache()` does not cache response bodies. It emits cache headers. Browsers, proxies, CDNs, or your own infrastructure decide whether to store and reuse the HTTP response.
:::

You can also express cache metadata in SDL:

```graphql
type Query {
  productById(id: ID!): Product @cacheControl(maxAge: 300, sharedMaxAge: 900)
}
```

Or with descriptors:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType(d =>
        d.Name("Query")
            .Field("productById")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Resolve("Product")
            .CacheControl(maxAge: 300, sharedMaxAge: 900));
```

## Configure cache-control defaults

`ModifyCacheControlOptions` controls how defaults are applied.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
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

| Option          | Default  | Meaning                                                                   |
| --------------- | -------- | ------------------------------------------------------------------------- |
| `Enable`        | `true`   | Enables cache-control header generation.                                  |
| `DefaultMaxAge` | `0`      | Default `max-age` when defaults apply.                                    |
| `DefaultScope`  | `Public` | Default scope when defaults apply.                                        |
| `ApplyDefaults` | `true`   | Adds default cache metadata to eligible fields without explicit metadata. |

For production-oriented schemas, consider `ApplyDefaults = false`. Then fields opt in explicitly, and unannotated queries do not emit cache-control headers.

# Keep personalized data out of shared caches

HTTP cache headers apply to the whole GraphQL response. If one selected field is user-specific, the complete response must not be reused by a shared cache.

Mark personalized fields as private and vary by the headers that affect the response:

```csharp
using HotChocolate.Caching;

[QueryType]
public static class Query
{
    [CacheControl(60, Scope = CacheControlScope.Private, Vary = ["Authorization"])]
    public static UserProfile GetMe(IUserContext user)
        => user.Profile;
}
```

Expected headers:

```http
Cache-Control: max-age=60, private
Vary: authorization
```

Use `private` for fields such as `me`, account settings, viewer-specific prices, entitlements, tenant-scoped data, and admin data. Use `Vary` for request headers that change the response, such as `Authorization`, tenant, locale, or feature headers.

Avoid shared caching when cookies or implicit server-side user context affect the result. In those cases, use `private`, configure `no-store` outside Hot Chocolate when required, or do not opt those fields into cache control.

# Know how Hot Chocolate merges field cache policies

Hot Chocolate computes one response policy for query operations by walking the selected fields. It reads cache metadata on the field, falls back to the return type when values are missing, and recurses through child selections, including interface and union selections.

The merge is conservative:

| Constraint | Merge rule                                                     |
| ---------- | -------------------------------------------------------------- |
| `max-age`  | Lowest value wins.                                             |
| `s-maxage` | Lowest value wins when present.                                |
| Scope      | `private` wins over `public`.                                  |
| `Vary`     | Header names are normalized, merged, sorted, and deduplicated. |

For example, this operation selects one public product field and one private viewer field:

```graphql
query {
  productById(id: 1) {
    name
  } # public, max-age 300
  me {
    displayName
  } # private, max-age 60, Vary Authorization
}
```

The final response policy uses the most restrictive values:

```http
Cache-Control: max-age=60, private
Vary: authorization
```

Hot Chocolate emits no cache-control headers when any of these conditions apply:

- The operation is a mutation or subscription.
- The query contains introspection fields other than `__typename`.
- No selected field contributes `maxAge` or `sharedMaxAge`.
- The result contains GraphQL errors.
- Cache-control options are disabled.
- The request called `SkipQueryCaching()`.

# Skip cache-control for one request

Use an HTTP request interceptor when a request-specific condition should suppress cache-control header generation.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Caching;
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

Register the interceptor with the schema:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<NoCacheHeaderInterceptor>();
```

Only one HTTP request interceptor can be registered per schema. Put all request creation logic for that schema into the same interceptor type.

# Add resolver or domain caches when DataLoader is not enough

DataLoader coordinates work inside one request. It does not make repeated requests faster by itself. For cross-request reuse, put a domain cache behind a service that understands data ownership, authorization, and invalidation.

| Data                  | Good layer                              | Key dimensions                             | Invalidation                      |
| --------------------- | --------------------------------------- | ------------------------------------------ | --------------------------------- |
| Product details       | Domain cache plus optional CDN          | Product ID, locale, publication state      | Product update or catalog publish |
| Product catalog list  | Domain cache plus CDN                   | Filters, page arguments, locale, tenant    | Catalog publish or TTL            |
| Inventory             | Domain cache only or very short CDN TTL | SKU, location, tenant                      | Inventory event or short TTL      |
| Viewer-specific price | Private domain cache or no cache        | User segment, tenant, currency, product ID | Price or entitlement update       |
| `me` profile          | Private browser cache or no cache       | User ID                                    | Profile update or logout          |
| Admin dashboard       | No shared cache                         | User, role, tenant, filters                | Not applicable                    |

Cache domain objects or read models behind authorization-aware APIs. Avoid caching arbitrary resolver JSON shapes unless you can prove the shape, authorization context, and invalidation rules are stable.

Hot Chocolate mutations do not invalidate your domain cache automatically. Wire invalidation from writes, events, database change feeds, purge jobs, or TTLs.

# Choose TTLs and invalidation boundaries

A TTL is a risk decision. Longer TTLs reduce load, but they increase stale-data windows unless you have reliable purging.

| Data                         | Suggested layer                     | Example TTL                    | Invalidation                               |
| ---------------------------- | ----------------------------------- | ------------------------------ | ------------------------------------------ |
| Product catalog list         | CDN plus domain cache               | 5 to 15 minutes                | Purge on catalog publish.                  |
| Product detail               | CDN plus domain cache               | 5 minutes                      | Purge product key on update.               |
| Inventory                    | Domain cache only or very short CDN | 0 to 30 seconds                | Event or update driven.                    |
| Entitlements and permissions | No shared cache                     | None or very short private TTL | Permission update, session change, logout. |
| `me` profile                 | Private browser cache or none       | 0 to 60 seconds                | User update or logout.                     |
| Admin data                   | No shared cache                     | Not applicable                 | Not applicable.                            |

Separate the invalidation model by layer:

- Document and prepared operation caches are internal, memory-bound execution caches.
- Persisted operation invalidation is a deployment and versioning concern.
- Domain cache invalidation belongs to your application and data platform.
- CDN cache invalidation requires purge tooling or short TTLs.
- Client caches can stay stale after server and CDN invalidation, so define refetch or normalized-store update behavior for clients.

# Control memory and cache-key cardinality

Caching fails when keys are too fragmented or memory grows without bounds. Use this checklist before increasing sizes:

- Increase `OperationDocumentCacheSize` or `PreparedOperationCacheSize` only when repeated operations are evicted too quickly and memory is acceptable.
- Prefer persisted or trusted operations for high-traffic clients so operation IDs stay stable.
- Canonicalize variables for GET and CDN cache keys where possible.
- Avoid putting high-cardinality per-user headers in shared `Vary`. If the response varies by user, make it `private`.
- Bound application caches with size limits, TTLs, and eviction metrics.
- Watch DataLoader key counts for operations that request thousands of unique IDs. Batching does not make unbounded fan-out free.

A stable public CDN key usually has this shape:

```text
GET + host + /graphql/persisted/{operationId}/{operationName} + canonical variables + low-cardinality Vary headers
```

A poor shared-cache key varies by user, raw query text, inconsistent JSON formatting, cookies, or many request headers.

# Measure cache effectiveness

Measure each layer separately. A high hit rate in one layer can hide stale data or cache misses in another layer.

| Layer                    | Signals to watch                                                                                          |
| ------------------------ | --------------------------------------------------------------------------------------------------------- |
| DataLoader               | Batch count, average batch size, duplicate key rate, backend call count per operation.                    |
| Document cache           | `RetrievedDocumentFromCache` and `AddedDocumentToCache` diagnostic or activity events.                    |
| Prepared operation cache | `RetrievedOperationFromCache` core diagnostics and `AddedOperationToCache` diagnostic or activity events. |
| Persisted operations     | Storage hit and miss rate, invalid key errors, not-found errors, APQ first-miss rate, storage latency.    |
| HTTP/CDN cache           | Hit ratio, origin request rate, p95/p99 latency, cache status header, bandwidth offload, purge count.     |
| Domain cache             | Hit ratio, eviction rate, stale-data incidents, backend latency.                                          |
| Client cache             | Network request rate, fetch-policy behavior, stale UI reports.                                            |

Use [Instrumentation](/docs/hotchocolate/v16/server/instrumentation) or [Diagnostics events](/docs/hotchocolate/v16/operations/observability/diagnostics-events) when you need custom metrics from Hot Chocolate execution events. Use CDN and application metrics for layers outside the server.

# Troubleshoot common caching failures

Start with the symptom, identify the layer, then change the smallest thing that proves the cause.

| Symptom                                          | Likely layer                 | Check                                                                                                                                                                    | Fix                                                                                                                  |
| ------------------------------------------------ | ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| No `Cache-Control` header                        | Hot Chocolate cache control  | Is `HotChocolate.Caching` installed? Are `UseQueryCache()` and `AddCacheControl()` registered? Does the selected query have cache metadata when `ApplyDefaults = false`? | Add the package and middleware, annotate fields, or enable defaults intentionally.                                   |
| No `Cache-Control` header for an annotated field | Execution result             | Is the operation a mutation, subscription, introspection query, or a result with GraphQL errors? Did an interceptor call `SkipQueryCaching()`?                           | Cache only successful query responses and remove the skip condition when appropriate.                                |
| Header exists but CDN does not cache             | CDN or HTTP key              | Does the request use POST? Is the response `private`? Do cookies or auth headers force bypass? Does the CDN include query string parameters?                             | Use GET for public queries, configure the CDN route and key, and avoid shared caching for private data.              |
| CDN misses for the same operation                | Cache key                    | Are variables encoded differently? Do clients send full query text? Do request headers differ?                                                                           | Use persisted operation IDs and canonical variable encoding. Keep `Vary` low-cardinality.                            |
| Stale data                                       | Domain, CDN, or client cache | Is the TTL too long? Are purge events wired to writes? Is the client normalized cache reusing records?                                                                   | Shorten TTLs, add purge or event-driven invalidation, and document client refetch behavior.                          |
| Personalized data appears in a shared cache      | Privacy boundary             | Is the field missing `private`? Does the resolver depend on implicit user context? Is the CDN caching authenticated responses?                                           | Mark fields private, add required `Vary`, or bypass shared caching for authenticated traffic.                        |
| Persisted operation not found                    | Persisted operation storage  | Was the document uploaded? Does the client hash algorithm and format match? Is this the first APQ optimized request?                                                     | Upload the document, align hash settings, or let APQ perform the first miss and store flow.                          |
| Persisted operation invalid key                  | Request shape                | Does the URL contain characters that are invalid for a document ID? Is the client sending Relay `doc_id` instead of Hot Chocolate `id` in POST JSON?                     | Send the expected `id` field or use the persisted route with a valid operation ID.                                   |
| Memory growth or churn                           | Key cardinality              | Are cache sizes too high? Are clients generating dynamic query text or many unique variables? Are application caches unbounded?                                          | Bound cache sizes, use persisted operations, normalize keys, and add eviction metrics.                               |
| GET request rejected or not cached               | HTTP transport or CDN        | Is a mutation sent over GET? Is the URL too long or not URL-encoded? Does the CDN strip query parameters?                                                                | Send only query operations over GET, encode parameters, use persisted routes, and configure query-string forwarding. |

# Verification checks

After you change caching, verify behavior with concrete requests.

1. Run the same public query twice and confirm the origin receives fewer downstream calls, or the CDN reports a hit on the second request.
2. Inspect headers for public data:

```http
Cache-Control: max-age=300, s-maxage=900
```

3. Inspect headers for personalized data:

```http
Cache-Control: max-age=60, private
Vary: authorization
```

4. Send a query with a resolver error and confirm Hot Chocolate does not emit cache-control headers.
5. Change underlying data and confirm the domain cache, CDN cache, and client cache all refresh according to your documented invalidation path.
6. Watch memory, eviction, and hit-rate metrics before increasing cache sizes.

# Next steps

- [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for request-scoped batching and key caching.
- [Trusted documents](/docs/hotchocolate/v16/operations/security-hardening/trusted-documents) for production operation allow lists.
- [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents) for operation storage setup.
- [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for runtime operation storage.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for GET, POST, and response formatting.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for request-level overrides such as `SkipQueryCaching()`.
- [Options](/docs/hotchocolate/v16/api-reference/options) for schema and request option references.
- [Diagnostics events](/docs/hotchocolate/v16/operations/observability/diagnostics-events) for custom cache metrics.
