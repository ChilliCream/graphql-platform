---
title: "Resolvers"
---

A resolver is the function that produces the value for one field in your GraphQL schema. Every field selected by a client, whether it reads a property, calls a service, or loads related data, is backed by a resolver. This page explains the resolver mental model, shows how Hot Chocolate discovers and runs resolvers, and routes deeper details to focused child pages.

## What you will learn

- How a client request becomes a tree of resolver calls.
- Which C# members become schema fields and how names are derived.
- Which resolver path to choose for each task.
- Which parameters are GraphQL arguments and which are injected by Hot Chocolate.
- How to keep resolvers thin and avoid N+1 data access.
- Where to find focused guidance for signatures, parent access, service injection, errors, and more.

---

## The resolver mental model

When a client sends a query, Hot Chocolate walks the selection set and calls a resolver for each selected field. Each resolver returns a value. Child field resolvers receive the parent resolver's value and produce their own values. The process continues until every selected leaf field is resolved.

Consider this query against a product catalog:

```graphql
query GetProducts {
  products(first: 3) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

Hot Chocolate executes a resolver tree for that query:

```
Query.products          (root resolver, runs first)
  ProductsConnection.nodes
    Product.name        (sibling fields run in parallel)
    Product.brand       (sibling fields run in parallel)
      Brand.name        (runs after brand)
```

**Execution rules to keep in mind:**

- A parent resolver must finish before its child field resolvers start.
- Sibling fields on `Query` and on object types can run in parallel during query execution.
- Top-level mutation fields run serially in document order.
- Child fields of a mutation payload use normal parallel field execution.

> **Watch out:** Do not store per-request mutable state on GraphQL type class instances. GraphQL type classes are shared across requests. Do not produce side effects in query resolvers that depend on sibling execution order because sibling fields can run concurrently.

---

## How Hot Chocolate finds a resolver

Hot Chocolate discovers resolvers from your C# code. Source generation is the primary approach for implementation-first schemas. Code-first uses the fluent descriptor API.

### Properties resolve simple fields

Any public property with a getter on a runtime type resolves to a schema field automatically. No attribute is required.

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
```

This produces the following SDL:

```graphql
type Product {
  id: UUID!
  name: String!
  price: Decimal!
}
```

### Methods resolve computed or fetched fields

Public methods become field resolvers. The source generator strips the `Get` prefix and `Async` suffix, then converts the name to GraphQL camelCase. Use `[GraphQLName]` to override the generated name.

Place root query methods in a class marked with `[QueryType]`:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<IReadOnlyList<Product>> GetProductsAsync(
        int first,
        ProductService products,
        CancellationToken ct)
        => await products.GetTopAsync(first, ct);
}
```

The `Get` prefix and `Async` suffix are stripped, producing a `products` field on the `Query` type:

```graphql
type Query {
  products(first: Int!): [Product!]!
}
```

Place computed fields on an object type using `[ObjectType<T>]` or `[ExtendObjectType<T>]`:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static string GetDisplayLabel([Parent] Product product)
        => $"{product.Name} ({product.Price:C})";
}
```

This adds a `displayLabel` field to `Product`. The `[Parent]` attribute injects the resolved product. See [Parent access](./parent-attribute) for details.

### Code-first alternative

If you prefer the fluent API, use `ObjectType<T>` with `.Field(...)` and `.Resolve(...)`:

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field("displayLabel")
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                return $"{product.Name} ({product.Price:C})";
            });
    }
}
```

For the full resolver signature reference, including supported return types and naming rules, see [Resolver Signature](./resolver-signature).

---

## Choose the right resolver path

| Task                                               | Recommended approach                                                  | Detail                                                                                                      |
| -------------------------------------------------- | --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Add a root read field                              | `[QueryType]` method                                                  | [Resolver Signature](./resolver-signature), [Queries](/docs/hotchocolate/v16/building-a-schema/queries)     |
| Add a root write field                             | `[MutationType]` method                                               | [Resolver Signature](./resolver-signature), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations) |
| Expose simple object data                          | Public property on the runtime type                                   | [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types)                                       |
| Compute a field from the current object            | Method on runtime type, `[ObjectType<T>]`, or `[ExtendObjectType<T>]` | [Parent access](./parent-attribute)                                                                         |
| Load related data for many parent objects          | DataLoader-backed nested resolver                                     | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                          |
| Batch one field-specific operation                 | `[BatchResolver]` or `.ResolveBatch(...)`                             | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                          |
| Read per-request tenant, user, or correlation data | Interceptor plus global state                                         | [HTTP Context and State](./ihttpcontextaccessor-and-context)                                                |
| Inject application logic                           | Implicit service parameter                                            | [Service Injection](./service-injection)                                                                    |
| Report errors or partial batch results             | `GraphQLException`, `ReportError`, domain errors, or `ResolverResult` | [Result Handling](./resolver-result-handling)                                                               |
| Add reusable cross-cutting field behavior          | Field middleware                                                      | [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)                                |

---

## Parameters at a glance

Resolver method parameters are either GraphQL arguments or values injected by Hot Chocolate. The table below classifies each kind:

| Parameter kind         | Example                                        | Appears in schema?  | Detail                                                             |
| ---------------------- | ---------------------------------------------- | ------------------- | ------------------------------------------------------------------ |
| GraphQL argument       | `string search`, `int first`                   | Yes                 | [Parameter Attributes](./parameter-attributes)                     |
| Typed ID argument      | `[ID<Product>] Guid id`                        | Yes, as `ID` scalar | [Parameter Attributes](./parameter-attributes)                     |
| Parent value           | `[Parent] Product product`                     | No                  | [Parent access](./parent-attribute)                                |
| Implicit service       | `ProductService products` (registered in DI)   | No                  | [Service Injection](./service-injection)                           |
| Keyed service          | `[Service("catalog")] ProductService products` | No                  | [Service Injection](./service-injection)                           |
| DataLoader             | `IBrandByIdDataLoader brandById`               | No                  | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| Global state           | `[GlobalState("TenantId")] string tenantId`    | No                  | [HTTP Context and State](./ihttpcontextaccessor-and-context)       |
| Resolver context       | `IResolverContext context`                     | No                  | [Resolver Signature](./resolver-signature)                         |
| Cancellation           | `CancellationToken ct`                         | No                  | [Resolver Signature](./resolver-signature)                         |
| Selection optimization | `[IsSelected("address")] bool includeAddress`  | No                  | [Parameter Attributes](./parameter-attributes)                     |

> **Watch out:** A parameter becomes a GraphQL argument unless Hot Chocolate recognizes it as a service or a known special type. If a service parameter appears as a schema argument, confirm that the service type is registered in the DI container. See [Service Injection](./service-injection) for disambiguation.

---

## Keep resolvers thin

A resolver should do one job: collect its inputs and delegate to the layer that owns the logic.

**What belongs in a resolver:**

- Read GraphQL arguments and parent values.
- Call a service, query handler, repository, EF Core query, or DataLoader.
- Pass `CancellationToken` through to async operations.
- Map application results to GraphQL values or error responses.

**What does not belong in a resolver:**

- Multi-step business workflows.
- Repeated per-item database access for nested fields.
- Request initialization or session setup.
- Long-lived mutable state.
- HTTP-specific assumptions for fields that should work across transports.

**Before:**

```csharp
// Brand resolver that queries the database once per product: N+1 problem
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    CatalogContext db,
    CancellationToken ct)
    => await db.Brands.FindAsync(product.BrandId, ct);
```

**After:**

```csharp
// Brand resolver that uses a DataLoader: one batched query for all products
public static async Task<Brand?> GetBrandAsync(
    [Parent] Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken ct)
    => await brandById.LoadAsync(product.BrandId, ct);
```

---

## Async, cancellation, and execution behavior

Most data-fetching resolvers return `Task<T>`. Add `CancellationToken` as a parameter and Hot Chocolate cancels it when the client disconnects or the request times out. Pass the token to every async call: EF Core queries, HTTP clients, service calls, and DataLoader `LoadAsync`.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<IReadOnlyList<Product>> GetProductsAsync(
        int first,
        ProductService products,
        CancellationToken ct)
        => await products.GetTopAsync(first, ct);
}
```

**Scoped service behavior:**

- During query execution, services are resolver-scoped by default. Each resolver gets its own scope, which allows sibling fields to run in parallel safely.
- During mutation execution, services are request-scoped by default. All resolvers in the mutation share one scope, which preserves transactional semantics.

For full details on scoping, `[UseRequestScope]`, and keyed services, see [Service Injection](./service-injection).

---

## Avoid N+1 and over-fetching

Nested resolvers create N+1 problems when each resolver fires a separate database query for every parent item. The fix is to batch those lookups with a DataLoader.

### Define a DataLoader

Use the `[DataLoader]` attribute and the source generator. The generator creates the `IBrandByIdDataLoader` interface and registers the implementation automatically.

```csharp
// DataLoaders/BrandDataLoaders.cs
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
```

### Use the DataLoader in a resolver

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
        => await brandById.LoadAsync(product.BrandId, ct);
}
```

**Query count comparison for three products, each with a brand:**

| Approach                                         | Database queries                                  |
| ------------------------------------------------ | ------------------------------------------------- |
| Resolver calls `db.Brands.FindAsync` per product | 1 product query + 3 brand queries = 4 total       |
| DataLoader batches all brand keys                | 1 product query + 1 batched brand query = 2 total |

The DataLoader collects all brand keys from the concurrent resolver wave, then fires one `WHERE id IN (...)` query. For full DataLoader documentation, see [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).

**Additional strategies to consider:**

- Use paging, filtering, sorting, and projections for collection fields backed by query providers. These tools push filtering and shaping to the database layer.
- Use `[BatchResolver]` or `.ResolveBatch(...)` for one-off field-specific batch operations that do not need a shared DataLoader.
- Use `[IsSelected]` as an advanced conditional-fetching optimization when you need to skip an expensive operation that the client did not select.

---

## Request state and HTTP boundaries

To share per-request values such as tenant ID, user identity, or correlation IDs across resolvers, initialize that state in an HTTP or WebSocket interceptor and store it on the request:

```csharp
// In your IHttpRequestInterceptor implementation
public override ValueTask OnCreateAsync(
    HttpContext httpContext,
    IRequestExecutor requestExecutor,
    OperationRequestBuilder requestBuilder,
    CancellationToken ct)
{
    var tenantId = httpContext.Request.Headers["X-Tenant-Id"].ToString();
    requestBuilder.SetProperty("TenantId", tenantId);
    return base.OnCreateAsync(
        httpContext,
        requestExecutor,
        requestBuilder,
        ct);
}
```

Read the value in any resolver with `[GlobalState]`:

```csharp
public static async Task<IReadOnlyList<Product>> GetProductsAsync(
    [GlobalState("TenantId")] string tenantId,
    ProductService products,
    CancellationToken ct)
    => await products.GetByTenantAsync(tenantId, ct);
```

**Boundary guidance:**

- Use `IHttpContextAccessor` only for HTTP-specific details such as request headers or cookies.
- Prefer global state, services, and resolver parameters for logic that should work across HTTP, WebSocket, and other transports.

For setup details, interceptor configuration, and `IResolverContext` APIs, see [HTTP Context and State](./ihttpcontextaccessor-and-context).

---

## Results and errors at a glance

| Situation                   | Recommended approach                                                              |
| --------------------------- | --------------------------------------------------------------------------------- |
| Successful field value      | Return the value from the resolver                                                |
| Field returns nothing       | Return `null` for a nullable field; throw or return an error for a non-null field |
| Field-level GraphQL error   | Throw `GraphQLException` or call `IResolverContext.ReportError`                   |
| Expected business failure   | Use mutation payload types with a domain error union field                        |
| Partial batch result errors | Use `ResolverResult` with per-item error entries                                  |

A field error produces a `null` field and an entry in the top-level `errors` array:

```json
{
  "data": {
    "products": {
      "nodes": [
        { "name": "Widget", "brand": null },
        { "name": "Gadget", "brand": { "name": "Acme" } }
      ]
    }
  },
  "errors": [
    {
      "message": "Brand not found.",
      "locations": [{ "line": 5, "column": 7 }],
      "path": ["products", "nodes", 0, "brand"]
    }
  ]
}
```

For null propagation rules, `IError` construction, `ResolverResult`, and mutation error conventions, see [Result Handling](./resolver-result-handling).

---

## Field middleware and resolvers

Field middleware wraps field resolution with reusable behavior: authorization checks, caching, instrumentation, or input validation. A resolver gets a value. Middleware decides whether to call the resolver, inspect its result, or replace it.

`IMiddlewareContext` extends `IResolverContext`, so middleware has access to the same resolver context. Middleware can read and write the `Result` property before or after the resolver runs.

Use middleware for cross-cutting concerns, not for ordinary data loading. Mixing data access into middleware couples it to a specific field's shape and makes reuse harder.

For pipeline order and `UseField` registration, see [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware).

---

## Troubleshooting

| Symptom                                                   | Likely cause                                              | Fix                                                               | Detail                                                             |
| --------------------------------------------------------- | --------------------------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------ |
| A service parameter appears as a GraphQL argument         | Service type is not registered in DI                      | Register the service or use `[Service("key")]` for keyed services | [Service Injection](./service-injection)                           |
| Nested field fires many database queries                  | N+1 resolver pattern                                      | Use DataLoader or a batch resolver                                | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| Query resolver has threading issues with a scoped service | Query fields run concurrently                             | Confirm resolver-scoped service defaults and DI scope overrides   | [Service Injection](./service-injection)                           |
| Resolver cannot read tenant, user, or correlation data    | State was not set before execution                        | Use an interceptor to write global state before the request runs  | [HTTP Context and State](./ihttpcontextaccessor-and-context)       |
| Mutation side effects run in an unexpected order          | Only top-level mutation fields are serialized             | Keep side effects in top-level mutation resolvers                 | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations)    |
| Client receives `Unexpected Execution Error`              | Unhandled exception not translated into a GraphQL error   | Use `GraphQLException`, `ReportError`, or domain payload errors   | [Result Handling](./resolver-result-handling)                      |
| Batch resolver result maps to wrong parent items          | Result list count or order does not match the parent list | Preserve order and count, or use the dictionary keyed pattern     | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |

---

## Where to go next

| Goal                                                                                      | Page                                                                                                                                                                                                                                                                  |
| ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Understand supported return types, naming rules, and the full method signature            | [Resolver Signature](./resolver-signature)                                                                                                                                                                                                                            |
| Access parent values in nested field resolvers                                            | [Parent access](./parent-attribute)                                                                                                                                                                                                                                   |
| Inject services and understand resolver versus request scope                              | [Service Injection](./service-injection)                                                                                                                                                                                                                              |
| Use argument and parameter attributes such as `[ID]`, `[GraphQLName]`, and `[IsSelected]` | [Parameter Attributes](./parameter-attributes)                                                                                                                                                                                                                        |
| Access request state, `IResolverContext`, and HTTP-specific data                          | [HTTP Context and State](./ihttpcontextaccessor-and-context)                                                                                                                                                                                                          |
| Handle nulls, errors, and batch result errors                                             | [Result Handling](./resolver-result-handling)                                                                                                                                                                                                                         |
| Batch data loading and avoid N+1                                                          | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)                                                                                                                                                                                                    |
| Add cross-cutting field behavior                                                          | [Field Middleware](/docs/hotchocolate/v16/execution-engine/field-middleware)                                                                                                                                                                                          |
| Place fields on Query, Mutation, and object types                                         | [Schema Elements](/docs/hotchocolate/v16/build2/schema-elements), [Queries](/docs/hotchocolate/v16/building-a-schema/queries), [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations), [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types) |
