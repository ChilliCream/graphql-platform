---
title: "Performance mental model"
description: "Reason about Hot Chocolate v16 performance from operation shape to resolver fan-out, data provider translation, request limits, caching, and production telemetry."
---

# Performance Mental Model

Why does a GraphQL request slow down even when each resolver appears fast on its own? The answer lies in understanding how the shape of a GraphQL operation determines its performance. Unlike a REST endpoint with a fixed cost, a GraphQL request's cost depends on the fields selected, nesting, list sizes, use of aliases and fragments, resolver fan-out, batching, data provider translation, response size, and operational policies.

This page provides a mental model for analyzing performance before tuning Hot Chocolate options. Use it alongside [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/), [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/), and the [Performance tuning guide](/docs/hotchocolate/v16/guides/performance/).

## Start with the Shape of the Operation

Begin by examining the operation and tracing the data path:

```graphql
query SlowDashboard($first: Int!) {
  users(first: $first) {
    nodes {
      id
      name
      profileSummary
      orders(first: 20) {
        nodes {
          id
          total
          items(first: 10) {
            nodes {
              product {
                name
                reviews(first: 5) {
                  nodes {
                    rating
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

Several performance factors are at play in this operation:

| Shape            | What to Inspect                                              | Why It Matters                                                                 |
|------------------|-------------------------------------------------------------|--------------------------------------------------------------------------------|
| Query shape      | Selection depth, repeated relationships, aliases, fragments, page sizes, requested fields | The client determines which schema parts execute for this request.               |
| Data shape       | Relationship cardinality, indexes, column width, external service calls | Even a small selection can touch many rows or slow services.                    |
| Resolver shape   | Per-field calls, async boundaries, materialization points, batching opportunities | A fast resolver can become expensive when repeated many times.                  |
| Sibling shape    | The same field selected for many parent objects              | A child field may run once per parent unless batched.                           |
| Operations shape | Public/private surface, known/unknown operations, request size, cache hit rate | Different controls are needed for repeated and unknown documents.               |

Label the slowest part of the path before choosing a performance feature:

| Signal                        | Likely Source                  | Next Step                                                                 |
|-------------------------------|--------------------------------|--------------------------------------------------------------------------|
| Many similar SQL statements   | Key-based relationship fan-out | Add or verify [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) usage. |
| Computed field runs for every item in a list | Sibling-field fan-out           | Consider a batch resolver for that field.                                 |
| One SQL query returns many columns or rows | Provider translation or early materialization | Review [projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/). |
| Large nested result payload   | List shape and response size   | Add page limits and review client selection.                              |
| High parse, validation, or compile time for repeated documents | Request overhead                  | Review [operation caching](/docs/hotchocolate/v16/guides/performance/) and [persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/). |
| Unknown expensive documents reach production | Operations policy                  | Add [request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/). |

A typical investigation follows this order:

1. Reduce fan-out.
2. Batch repeated sibling work.
3. Push filtering, sorting, paging, and selection to the provider.
4. Bound the work clients can request.
5. Reduce repeated request overhead for known operations.
6. Measure the result before tuning another layer.

## Diagnose Before Choosing a Performance Feature

Do not begin by changing cache sizes or adding middleware everywhere. Instead, let the symptom guide your next step:

| Symptom | Likely Cause | First Check | Likely Feature | Read Next |
|---------|--------------|-------------|---------------|-----------|
| The database log shows one query for the list and one per row | N+1 relationship loading | Which key does each child resolver load? | DataLoader | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| A field like `profileSummary` runs for every `User` in a list | Sibling-field fan-out | Can the field be resolved for all sibling parents together? | Batch resolver | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/#batch-resolvers) |
| The database returns full entities when only a few fields are selected | Over-fetching or early materialization | Does the resolver return `IQueryable<T>` or `QueryContext<T>` at the right boundary? | Projections and provider translation | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) |
| Filtering or sorting happens after `ToListAsync` | Work moved into application memory | Where is the first materialization point? | Filtering, sorting, paging over provider-backed data | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) |
| Clients can request deeply nested lists with large page sizes | Unbounded operation shape | What is the supported query envelope? | Request limits and cost analysis | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) |
| The same large operation text repeats on every request | Repeated document overhead | Are document and operation caches hitting? | Operation caching, persisted operations, trusted documents | [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/) |
| Unsure if time is spent in GraphQL, SQL, HTTP, or a downstream service | Missing evidence | Do traces include request, resolver, DataLoader, and dependency timing? | Instrumentation and operation monitoring | [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) |

For real-world operation monitoring, use [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/) to compare latency, resolver timing, errors, traces, and client or version attribution. Nitro provides evidence for tuning decisions. Use [operation reporting](/docs/nitro/apis/operation-reporting/) to send operation metadata to Nitro.

## Batch Resolvers for Sibling Field Fan-Out

A standard resolver runs for each parent object that selects a field. For example, if `users(first: 100)` selects `profileSummary`, a non-batched resolver may run 100 times:

```graphql
query UserCards {
  users(first: 100) {
    nodes {
      id
      profileSummary
    }
  }
}
```

A batch resolver can resolve the same field for a group of sibling parent objects in one call. In v16, define one with `[BatchResolver]`, `.ResolveBatch(...)`, or `.ResolveBatchWith(...)`:

```
Before: 100 User parent objects -> 100 profileSummary resolver calls
After:  100 User parent objects -> 1 profileSummary batch resolver call
```

Batch resolvers provide field-local batching. The parent and returned values must align, so the resolver returns one result per parent in the same order. See [DataLoader and batch resolver reference](/docs/hotchocolate/v16/resolvers-and-data/dataloader/#batch-resolvers) for method signatures.

**Good fits:**
- A computed value can be produced for a group of parents.
- An external service has a batch endpoint for the field.
- The value belongs to one selected field and does not need request-wide key caching.

**Poor fits:**
- The same entity lookup is shared by several resolvers.
- The field is a broad list query needing paging, filtering, sorting, or projection.
- The result should be cached across different paths in the same request.

## DataLoader for Key-Based Request Batching and Caching

Field-level composition can cause repeated data fetches. For example, a list resolver returns users, and each `orders` resolver loads orders for one user:

```graphql
query UsersWithOrders {
  users(first: 3) {
    nodes {
      id
      orders {
        id
        total
      }
    }
  }
}
```

Without batching, the data path might look like:

```
1 query:  load users
3 queries: load orders for user 1, user 2, and user 3
```

A DataLoader batches keys collected during execution and returns results to the requesting resolvers:

```
1 query: load users
1 query: load orders where userId in (1, 2, 3)
```

DataLoader also provides request-scoped caching. If two resolver paths load the same key in one request, DataLoader returns the same cached task instead of repeating the data source call. This cache is not shared across requests.

**Good fits:**
- Load related entities by ID.
- Call a service that supports batch requests.
- Deduplicate repeated key loads in one operation.
- Share the same lookup across different resolver paths.

**Poor fits:**
- A broad collection query where the client controls `where`, `order`, and page arguments.
- A replacement for database indexes or provider-side filtering.
- A durable cross-request cache. Use application or data-access boundaries for that, with tenant, authorization, invalidation, and observability rules.

See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) for implementation, and [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) for data-access placement.

## Choosing Between Batch Resolver and DataLoader

Choose based on the shape of the problem:

| Symptom | Best First Tool | Why | Link |
|---------|----------------|-----|------|
| One selected field repeats for many sibling parents | Batch resolver | The work belongs to one field execution shape. | [Batch resolvers](/docs/hotchocolate/v16/resolvers-and-data/dataloader/#batch-resolvers) |
| Many `authorId` or `customerId` lookups across fields | DataLoader | The work is key-based and benefits from request-scoped deduplication. | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) |
| A large list returns full rows while the client selects a few columns | Projections and provider translation | The server should load less data before batching is relevant. | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) |
| A nested list can expand without a page bound | Paging and cost limits | The supported operation envelope must be defined before execution. | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) |

You can combine these tools. A batch resolver may call an application service directly or delegate key lookups to a DataLoader when request-level caching is needed. Neither replaces provider-side projection, filtering, sorting, or paging for broad collection queries.

## Push Filtering, Sorting, Paging, and Selection to the Data Provider

Let the database or provider handle as much work as possible. If the provider can filter, sort, page, or project, let it do so before data reaches application memory.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<User> GetUsers(CatalogContext db)
        => db.Users.Where(u => u.IsActive);
}
```

For a request like this:

```graphql
query ActiveUsers {
  users(
    first: 10
    where: { name: { contains: "ada" } }
    order: [{ name: ASC }]
  ) {
    nodes {
      name
      email
    }
  }
}
```

The ideal behavior is:
- Apply server-owned rules, such as `IsActive`, before client shaping.
- Apply filtering and sorting before materialization.
- Apply paging before returning the result.
- Select only the fields needed for the GraphQL selection, where the provider supports projection.

GraphQL selections and database projections are related but not identical. GraphQL describes the response shape, while the provider decides which parts can be translated into SQL, MongoDB queries, service calls, or other native formats.

`IQueryable<T>` is a capability boundary, not a guarantee. It allows middleware to compose provider-backed shapes, but a provider may not translate custom .NET methods, computed properties, or certain expressions. Measure the generated query or provider diagnostics to confirm translation.

Watch for early materialization:

```csharp
public static async Task<List<User>> GetUsersAsync(
    CatalogContext db,
    CancellationToken ct)
    => await db.Users.Where(u => u.IsActive).ToListAsync(ct);
```

This resolver returns correct data but runs the database query before Hot Chocolate data middleware can compose provider-backed filtering, sorting, paging, or projection. Move the materialization point later if provider translation is the goal.

When combining data middleware in v16, apply it in this order:

```
UsePaging > UseProjection > UseFiltering > UseSorting
```

See [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) for the execution model and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/) for details on `IQueryable<T>` and `QueryContext<T>`.

## Bound the Work Clients Can Request

Performance and safety depend on the supported query envelope. Every API needs limits, including private APIs, as internal teams can create costly operations unintentionally.

Start with list boundaries. Nested operations can multiply quickly:

```graphql
query ExpandingShape {
  users(first: 50) {
    nodes {
      orders(first: 50) {
        nodes {
          items(first: 50) {
            nodes {
              product {
                reviews(first: 50) {
                  nodes {
                    rating
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

This shape can request up to `50 x 50 x 50 x 50` nested nodes, not counting scalar fields, authorization checks, resolver calls, serialization, or downstream work.

Use several guardrails together:

| Guardrail                | What It Protects                | Where to Continue |
|--------------------------|---------------------------------|-------------------|
| Maximum page size, default page size | List cardinality                | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) |
| Parser limits            | Large documents, tokens, fields, directives, recursion during parsing | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) |
| Validation limits        | Deep execution paths, fragment visits, field merge comparisons, field cycles | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) |
| Cost analysis            | Field cost and type/data cost before execution | [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) |
| Trusted documents        | Known operation policy for controlled clients | [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) |

Cost analysis is not the same as optimizing a hot path. It rejects or reports operations that exceed a budget before resolver execution. Use the `GraphQL-Cost: report` workflow from the [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) page to measure expected operations, set budgets, and verify that rejection messages are clear for your clients.

## Reduce Repeated Request Overhead for Known Operations

While resolver and data work usually dominate slow operations, repeated request overhead can matter at scale.

The request pipeline looks like this:

```
transport bytes -> parse -> validate -> compile -> execute -> serialize
```

Hot Chocolate caches parsed documents and prepared operations after first use. Tuning cache size helps with repeated operation documents, but does not make expensive resolver work faster. If most time is spent in SQL or a downstream service, increasing the operation cache size will not address the main cost. Transport payload size also affects repeated overhead, as every request and response still moves bytes over the network.

Trusted documents reduce repeated document overhead by executing pre-registered operations by ID or hash:

```
Build or publish: reviewed document -> register
Request: operation ID or hash -> document lookup -> execute
```

Automatic persisted operations use a miss, upload, and store workflow for dynamic clients:

```
First request:   hash -> miss
Upload request:  full document -> parse and validate -> store -> execute
Known request:   hash -> document lookup -> execute
```

Choose the right workflow:

| Need                                         | Consider                    | Why                                                        |
|-----------------------------------------------|-----------------------------|------------------------------------------------------------|
| Known first-party operations from a build pipeline | Trusted documents           | The server can execute reviewed documents by ID or hash.    |
| Dynamic clients that repeat operations        | Automatic persisted operations | The client can send a hash and upload the document after a miss. |
| Client and gateway release governance         | Nitro client registry        | Teams can publish known operations with client versions.    |
| Cold-start latency for representative operations | Server warmup                | Startup can populate caches before traffic.                 |

See [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/) for cache boundaries, [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) for trusted documents, [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) for APQ, [Warmup](/docs/hotchocolate/v16/server/warmup/) for startup cache population, and the [Nitro client registry](/docs/nitro/apis/client-registry/) with [Nitro client commands](/docs/nitro/cli-commands/client/) for operation publication workflows.

## Measure the Slowest Part, Then Tune the Smallest Surface

Use evidence before changing options.

Capture a representative operation and its variables. Run the same operation before and after each change. Record enough data to confirm or reject your hypothesis.

| Metric                        | Why Capture It                                                      |
|-------------------------------|---------------------------------------------------------------------|
| p50 and p95 latency           | Shows typical and tail behavior.                                    |
| Throughput under load         | Shows if a change improves one request but reduces total capacity.  |
| Managed allocation            | Reveals response shaping, materialization, and serialization pressure. |
| SQL or data-source call count | Reveals N+1, missing batching, and early materialization.           |
| Resolver timing               | Identifies field-level hotspots when enabled.                       |
| Batch resolver invocations    | Confirms repeated sibling work was grouped.                         |
| DataLoader batch/key count    | Confirms key loads were batched and deduplicated.                   |
| Response bytes                | Shows serialization and network pressure from large selections.      |
| Document/operation cache hit  | Separates request overhead from execution work.                     |
| Cost value                    | Confirms the operation fits the supported budget.                   |
| Client name and version       | Lets you attribute hot paths to releases or consumers.              |
| Trace link                    | Gives the team a shared artifact for review.                        |

Hot Chocolate supports diagnostic event listeners and OpenTelemetry. See [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) for setup, exported spans, DataLoader events, and cache events. Be careful with high-cardinality labels and per-field instrumentation overhead. The `ResolveFieldValue` diagnostic event is not enabled by default, as per-resolver events add overhead.

Use [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/) to compare production operation behavior, resolver latency, errors, traces, and client versions before and after a change.

When writing performance notes, use this format:

| Field         | Example                                                      |
|-------------- |-------------------------------------------------------------|
| Hypothesis    | `orders` creates one SQL query per user.                    |
| Change        | Add an orders-by-user DataLoader.                           |
| Verification  | SQL count changes from 101 to 2 for `UsersWithOrders`.      |
| Rollback      | p95 latency or database CPU gets worse due to batch plan.   |

## Putting the Model Together for a Slow Query

Apply the same review sequence when diagnosing a slow operation:

```graphql
query SlowDashboard {
  users(first: 100) {
    nodes {
      id
      profileSummary
      orders(first: 20) {
        nodes {
          id
          customer {
            name
          }
          items(first: 10) {
            nodes {
              product {
                name
              }
            }
          }
        }
      }
    }
  }
}
```

Review card:

| Checkpoint         | Question                                         | Possible Next Action                                                      |
|--------------------|--------------------------------------------------|---------------------------------------------------------------------------|
| Query shape        | Which selections and page sizes multiply work?    | Reduce page sizes or add stronger page limits.                            |
| Sibling-field fan-out | Does `profileSummary` run once per user?          | Convert the field to a batch resolver if it can be computed for all users.|
| Key-based fan-out  | Do `customer` or `product` resolvers load by ID? | Add or verify DataLoaders for customer and product IDs.                   |
| Provider translation | Do `users`, `orders`, or `items` materialize early? | Return provider-backed shapes and review generated queries.               |
| Guardrails         | Could a client request a deeper or wider version? | Set page limits, request limits, and cost budgets.                        |
| Request overhead   | Is this a known operation that repeats often?     | Consider warmup, trusted documents, APQ, or operation cache sizing.       |
| Measurement        | Which metric proves the change worked?            | Compare latency, data-source calls, batch counts, response bytes, and traces. |

The first fix is not always cache tuning. If an operation performs thousands of database calls, caching the parsed document will not address the main cost. If the operation repeats frequently and resolver work is already efficient, persisted operations or warmup may help.

## Next Steps

Choose your next topic based on the bottleneck you are facing:

- **Need the execution model?** [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/)
- **Need field and middleware placement?** [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/)
- **Need key-based batching?** [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/)
- **Need field-local batching?** [Batch resolvers](/docs/hotchocolate/v16/resolvers-and-data/dataloader/#batch-resolvers)
- **Need provider-backed data shaping?** [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/)
- **Need operation safety?** [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/)
- **Need request overhead and known-operation workflows?** [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/), [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/), [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/), [Warmup](/docs/hotchocolate/v16/server/warmup/)
- **Need telemetry?** [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/), [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/), [Nitro operation reporting](/docs/nitro/apis/operation-reporting/)
- **Need production tuning options?** [Performance tuning](/docs/hotchocolate/v16/guides/performance/), [Performance](/docs/hotchocolate/v16/performance/), [Server warmup](/docs/hotchocolate/v16/server/warmup/)

For GraphQL terminology, see the [GraphQL specification](https://spec.graphql.org/October2021/). For HTTP transport, see the [GraphQL over HTTP draft specification](https://graphql.github.io/graphql-over-http/draft/) and the Hot Chocolate [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) docs.
