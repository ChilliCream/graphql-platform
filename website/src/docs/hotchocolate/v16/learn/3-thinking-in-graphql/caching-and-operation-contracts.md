---
title: "Caching and operation contracts"
description: "Cache GraphQL at the right layer by treating stable operations, variables, identities, HTTP metadata, and trusted documents as separate contract boundaries."
---

# GraphQL and Caching

GraphQL supports caching, but the cache key is not the same as in REST. Many teams assume GraphQL cannot be cached because all requests go to a single endpoint. This is a misconception rooted in REST-based caching. In reality, a GraphQL response can change based on the operation document, selected operation, variables, request headers, user identity, tenant, locale, schema version, or underlying data.

The important question is not whether GraphQL can be cached, but rather: "At which layer is work repeated, and which inputs affect the result?"

# Cache Keys in GraphQL

REST APIs often use the URL and HTTP method as the cache key. In GraphQL, the URL alone is not enough, since the operation defines the response shape.

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
}
```

```json
{
  "id": "UHJvZHVjdDoxMjM="
}
```

To identify a GraphQL request, you need the operation text, operation name, and variables. The response may also depend on authorization, tenant, locale, preview mode, feature flags, and the negotiated response format.

| REST instinct | GraphQL equivalent | Key material |
| --- | --- | --- |
| Cache `GET /products/123` | Cache a complete query response | Operation document or hash, operation name, variables, relevant headers, identity, tenant, and freshness policy |
| Cache a resource object | Cache a normalized client record | Type name, stable object ID, fields read by the client, and invalidation rules |
| Reuse route handling work | Reuse prepared operation work | Document identity, selected operation, schema state, and operation cache settings |
| Allow only known routes | Allow only known operation documents | Operation hash, operation name, client version, and trusted document policy |

A single GraphQL endpoint does not prevent caching. It means the cache key must reflect the GraphQL operation, not only the endpoint URL.

# Caching at the Right Layer

GraphQL caching is most effective when applied in layers, with each layer targeting a different kind of repeated work.

| If you see this symptom | Start with this layer | What it caches | Read next |
| --- | --- | --- | --- |
| The same entity appears on many screens | Client normalized cache | Application records and operation results in the client | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Strawberry Shake caching](/docs/strawberryshake/v16/caching/) |
| The same operation document repeats | Document and operation caches | Parsed documents and prepared operations | [Performance tuning](/docs/hotchocolate/v16/guides/performance/) and [Warmup](/docs/hotchocolate/v16/server/warmup/) |
| Clients send the same large operation text | Persisted operations or APQ | Operation document lookup by hash | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) |
| The same safe query response repeats | HTTP response cache | A complete GraphQL response envelope | [Cache Control](/docs/hotchocolate/v16/server/cache-control/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| The same key loads many times during one execution | DataLoader | Request-scoped data lookup results | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| The backing service repeats expensive work across requests | Application or data cache | Domain data at the service boundary | [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) |

These layers can be combined, but they serve different purposes. For example, an operation cache can reduce parsing and preparation, but it will not speed up a slow database. DataLoader can deduplicate lookups within a single request, but does not persist data across requests.

# Client Normalized Caches for Application Data

GraphQL clients are often best positioned to cache application data. A normalized client store keeps objects by type and stable identity, usually using `__typename` and an ID or a client-specific key policy.

Suppose both a product list and a product detail screen display the same product:

```graphql
query GetProductCard($id: ID!) {
  productById(id: $id) {
    __typename
    id
    name
  }
}
```

If the client store recognizes this as `Product:123`, both screens can reference the same record. Mutations can update it, subscriptions can push changes, or the client can invalidate and refetch as needed.

This approach differs from HTTP response caching. A normalized store does not reuse an entire JSON response based on the URL. Instead, it reconstructs operation results from stored records. This makes schema design important: stable IDs, consistent type names, predictable nullability, connection patterns, and mutation payloads all help clients maintain correct local state.

Libraries like Strawberry Shake, Relay, Apollo Client, and urql Graphcache all use normalized caching, though their APIs and defaults vary. For .NET clients, start with [Strawberry Shake caching](/docs/strawberryshake/v16/caching/) and see how schema choices relate to [modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Reusing Operation Documents and Prepared Operations

When Hot Chocolate receives a request, it parses the operation document, validates it against the schema, prepares the operation, executes resolvers, and serializes the result.

If the same document is sent repeatedly, Hot Chocolate can reuse the parsed and prepared operation. The operation cache focuses on processing the operation, not on caching responses or data.

```text
First request: document -> parse -> validate -> prepare -> execute resolvers
Next request:  document -> reuse operation work -> execute resolvers
```

Consistent operation text and operation names improve cache hit rates, warmup, logs, traces, and client workflows. If a client sends an operation name, include it every time, as it is part of the operation's identity. The [Warmup](/docs/hotchocolate/v16/server/warmup/) page explains how to pre-populate document and operation caches for common requests.

For configuration details, see the [Performance tuning](/docs/hotchocolate/v16/guides/performance/) guide. Remember: an operation cache miss is not the same as a data cache miss.

# Persisted and Trusted Documents as Operation Contracts

With persisted operations, a client sends an operation identifier (often a hash) instead of the full operation text. The server looks up the document and executes it with the provided variables.

A trusted document policy takes this further. The server or gateway only accepts known operation documents in certain environments, turning the set of client operations into a deployable contract.

An operation contract consists of a known document and the variable shape that both client and server agree to use. The operation name helps with logs, traces, and registry reports. The operation hash identifies the exact document text. Variables are still sent with each execution and can affect authorization, response data, and cache variance.

| Term | What it identifies | What it does not identify by itself |
| --- | --- | --- |
| Operation name | A human-readable operation inside a document | Exact document text, variables, caller, or result freshness |
| Operation hash | Exact operation document text under a hash algorithm | Variable values, caller, tenant, or authorization outcome |
| Persisted operation | A stored document resolved by an identifier | A policy that rejects every ad-hoc operation unless configured that way |
| Trusted document | A known operation allowed by policy | A replacement for schema governance or authorization |
| Automatic persisted operation | A negotiation flow where a client can upload a missing document at runtime | A build-time known client operation set |

Choose the workflow that matches your needs:

| Need | Consider | Why |
| --- | --- | --- |
| Build-time known client operations | Trusted documents | The server can execute a reviewed, known operation set |
| Dynamic clients that repeat operations | Automatic persisted operations | Clients can send a hash on the optimized path and upload the document after a miss |
| A controlled first-party production surface | Trusted document policy plus schema governance | You can reject ad-hoc documents and review client operation changes |
| An open public API | Schema governance, cost controls, and optional persisted workflows | Unknown consumers may need to author operations you did not pre-register |

For setup, see [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/), the [First-party API guide](/docs/hotchocolate/v16/guides/private-api/), and the [Public API guide](/docs/hotchocolate/v16/guides/public-api/).

# Governing Client Operation Contracts with Nitro

Use Nitro to manage which client versions can run which operation documents against which schema version. Nitro validates, publishes, and monitors operation contracts across teams.

A typical workflow:

1. The client team collects operations such as `GetProduct`, `SearchProducts`, and `CreateOrder`.
2. Nitro validates the documents, fragments, variable shapes, and operation names against the target schema version.
3. The team publishes a client version, such as `web@42`, with the known operation hashes.
4. Servers and clients roll out in an order that avoids unknown-operation failures.
5. Production operation reports show which client versions and operations are still active.

This workflow is separate from APQ and from local normalized caches. APQ is about request negotiation. A normalized store caches application data in the client. Nitro manages schema and operation compatibility.

For more, see [Nitro client registry](/docs/nitro/apis/client-registry/), [Nitro operation reporting](/docs/nitro/apis/operation-reporting/), and [Nitro client commands](/docs/nitro/cli-commands/client/).

# HTTP GET and Response Caching

The [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/draft/) allows query operations to be sent with HTTP GET. Using GET can help browsers, reverse proxies, and CDNs reuse responses, provided your deployment supports stable URLs, safe query response reuse, and correct freshness metadata.

Complete response caching is more limited than client store or operation caching. HTTP headers apply to the entire GraphQL response, not to individual fields.

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProduct?variables=%7B%22id%22%3A%22UHJvZHVjdDoxMjM%3D%22%7D
Accept: application/graphql-response+json
```

The `/graphql/persisted/{operationId}/{operationName}` path is a Hot Chocolate convention for persisted operations. The GraphQL-over-HTTP specification defines parameters like `query`, `operationName`, `variables`, and `extensions`, but does not require a specific route path.

There are trade-offs with GET. URLs have length limits in browsers, proxies, CDNs, and servers. Query values such as JSON variables must be percent-encoded. Shared infrastructure may normalize, reject, truncate, or vary cache entries by query strings and headers in ways that do not match your GraphQL cache key. Always test your proxy or CDN before relying on response reuse.

Before allowing a shared HTTP cache or CDN to reuse a GraphQL response, consider:

| Question | Why it matters |
| --- | --- |
| Is the operation a query? | Mutations change state, and subscriptions are live streams, not reusable HTTP responses. |
| Are variables included in the key? | The same document can return different data for different variable values. |
| Does authorization change the result? | User-specific data must not leak through a shared cache. |
| Does tenant, locale, preview mode, or feature flag state change the result? | The cache key or `Vary` policy must separate different visible results. |
| Does `Accept` change the response format? | Content negotiation can change the response envelope and streaming behavior. |
| Do `ETag` or `Last-Modified` represent the complete response under the same variance rules? | Validators must describe the same response boundary as the cache key. |
| Can partial data with errors be reused? | A GraphQL response can contain both `data` and `errors`, so the policy must be deliberate. |

See [Cache Control](/docs/hotchocolate/v16/server/cache-control/) for details on `@cacheControl`, `[CacheControl]`, final `Cache-Control` and `Vary` header computation, and server setup. The [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) page covers GET, POST, request shape, response formats, and status code behavior.

# DataLoader and Data Caches for Repeated Data Work

DataLoader is designed for the request execution boundary. It batches and deduplicates repeated key loads within a single GraphQL execution.

Consider this query:

```graphql
query GetOrders {
  orders(first: 3) {
    nodes {
      id
      customer {
        id
        name
      }
    }
  }
}
```

If several orders reference the same customer ID, DataLoader can load that customer once per request and return the cached value to every resolver that needs it. This cache is request-scoped. Each new GraphQL request starts with a fresh DataLoader cache.

Longer-lived data caches belong at the application or data-access boundary. These require domain-specific invalidation rules, data ownership, authorization variance, tenant separation, and observability. Avoid hiding a global cross-user cache inside a resolver unless the key and invalidation rules are well defined.

Use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) for request-scoped batching and caching. For broader performance questions, see the [Performance mental model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/performance-mental-model/) to determine if the bottleneck is execution, resolver logic, provider translation, network calls, or database load.

# Set Invalidation and Safety Boundaries First

Expand cache scope only after you understand how results can change and who is permitted to see them. Shared caches require careful review, as an incorrect key can expose data across users or tenants.

| Boundary | Risk if omitted | Usual owner |
| --- | --- | --- |
| Operation document or hash | Different selections can reuse the wrong response | Client and GraphQL platform team |
| Operation name | Multi-operation documents, logs, and warmup can disagree | Client and platform team |
| Variables and field arguments | Different data can share one cache entry | Client, platform team, and cache owner |
| Authorization and user identity | Private data can leak through shared infrastructure | Security and platform team |
| Tenant, locale, preview mode, and feature flags | Users see data from the wrong context | Application team |
| Schema or deployment version | A cached response can outlive a contract change | Platform and release team |
| Partial data and errors | A temporary failure can become a cached result | API owner |
| Mutation and subscription invalidation | Client stores and data caches can remain stale | Application and client teams |
| Observability labels | High-cardinality key material can overload metrics | Platform and observability team |

Invalidation can be triggered by events, time, mutations, subscriptions, or manual actions. Mutations should update or invalidate client stores and relevant data caches. Subscriptions can push updates, invalidate local records, or trigger refetches.

For details on GraphQL response shape and partial data, see the [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) and the Hot Chocolate [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) page.

# Choosing a Cache Strategy

Before adding or expanding a cache, use this checklist:

| Step | Decision |
| --- | --- |
| 1. Identify repeated work | Is the repeated work UI reads, operation processing, request transport, DataLoader lookup, or backing-service load? |
| 2. Choose the narrowest layer | Select the layer that removes the repeated work without expanding the safety boundary unnecessarily. |
| 3. Define key material | Include operation document or hash, operation name, variables, entity IDs, headers, authorization, tenant, schema version, and format if they affect the result. |
| 4. Define freshness | Choose TTLs, validators, event rules, mutation rules, subscription updates, or manual purge steps. |
| 5. Define error behavior | Decide whether partial data, validation errors, execution errors, and transport failures can be reused. |
| 6. Verify representative cases | Test with different variables, users, tenants, locales, client versions, and schema versions. |
| 7. Link to the setup doc | Move from the concept to the Hot Chocolate, Nitro, Strawberry Shake, or infrastructure guide for the chosen layer. |

A sound strategy might look like this:

- Cache product records in the client store by type and ID.
- Use trusted documents for the production operation set.
- Do not put authenticated dashboard responses in shared caches.
- Use DataLoader for request-scoped customer lookup deduplication.

# Next Steps

- See [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) for details on operation documents, variables, and operation names.
- Explore [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Strawberry Shake caching](/docs/strawberryshake/v16/caching/) for client store behavior.
- Configure [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) or [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) when operation identity is part of your production policy.
- Use [Cache Control](/docs/hotchocolate/v16/server/cache-control/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) for response caching.
- Use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) for repeated data access within a single execution.
