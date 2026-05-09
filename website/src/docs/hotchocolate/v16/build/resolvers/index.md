---
title: "Resolvers"
---

A resolver is a function that provides the value for a specific field in your GraphQL schema. Every field selected by a client, whether it reads a property, calls a service, or loads related data, is powered by a resolver. This page introduces the resolver model, explains how Hot Chocolate discovers and executes resolvers, and points you to more detailed topics.

## What you will learn

- How a client request is processed as a tree of resolver calls
- Which C# members become schema fields and how their names are determined
- How to select the right resolver approach for each scenario
- Which parameters are GraphQL arguments and which are injected by Hot Chocolate
- How to keep resolvers focused and avoid N+1 data access issues
- Where to find in-depth guidance on signatures, parent access, service injection, error handling, and more

---

## The resolver model

When a client sends a query, Hot Chocolate traverses the selection set and invokes a resolver for each selected field. Each resolver returns a value. Child field resolvers receive the value from their parent and produce their own results. This process continues until all selected leaf fields are resolved.

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

Hot Chocolate executes a resolver tree for this query:

```
Query.products          (root resolver, runs first)
  ProductsConnection.nodes
    Product.name        (sibling fields run in parallel)
    Product.brand       (sibling fields run in parallel)
      Brand.name        (runs after brand)
```

**Key execution rules:**

- A parent resolver must complete before its child field resolvers begin.
- Sibling fields on `Query` and object types can execute in parallel during query processing.
- Top-level mutation fields execute serially in the order they appear in the document.
- Child fields of a mutation payload use normal parallel field execution.

> **Watch out:** Do not store per-request mutable state on GraphQL type class instances. These classes are shared across requests. Avoid side effects in query resolvers that depend on sibling execution order, as sibling fields may run concurrently.

---

## How Hot Chocolate discovers resolvers

Hot Chocolate identifies resolvers from your C# code. Source generation is the main approach for implementation-first schemas, while code-first uses the fluent descriptor API.

### Properties for simple fields

Any public property with a getter on a runtime type automatically becomes a schema field. No attribute is needed.

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

### Methods for computed or fetched fields

Public methods can serve as field resolvers. The source generator removes the `Get` prefix and `Async` suffix, then converts the name to GraphQL camelCase. You can use `[GraphQLName]` to override the generated name.

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

The `Get` prefix and `Async` suffix are removed, resulting in a `products` field on the `Query` type:

```graphql
type Query {
  products(first: Int!): [Product!]!
}
```

To add computed fields to an object type, use `[ObjectType<T>]` or `[ExtendObjectType<T>]`:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static string GetDisplayLabel([Parent] Product product)
        => $"{product.Name} ({product.Price:C})";
}
```

This adds a `displayLabel` field to `Product`. The `[Parent]` attribute injects the resolved product. See [Parent access](./parent-attribute) for more information.

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

For a complete reference on resolver signatures, supported return types, and naming rules, see [Resolver Signature](./resolver-signature).

---

## Choosing the right resolver approach

| Task                                               | Recommended approach                                                  | Detail                                                                                                                     |
| -------------------------------------------------- | --------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Add a root read field                              | `[QueryType]` method                                                  | [Resolver Signature](./resolver-signature), [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries)     |
| Add a root write field                             | `[MutationType]` method                                               | [Resolver Signature](./resolver-signature), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations) |
| Expose simple object data                          | Public property on the runtime type                                   | [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types)                                                  |
| Compute a field from the current object            | Method on runtime type, `[ObjectType<T>]`, or `[ExtendObjectType<T>]` | [Parent access](./parent-attribute)                                                                                        |
| Load related data for many parent objects          | DataLoader-backed nested resolver                                     | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                      |
| Batch one field-specific operation                 | `[BatchResolver]` or `.ResolveBatch(...)`                             | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                      |
| Read per-request tenant, user, or correlation data | Interceptor plus global state                                         | [HTTP Context and State](./ihttpcontextaccessor-and-context)                                                               |
| Inject application logic                           | Implicit service parameter                                            | [Service Injection](./service-injection)                                                                                   |
| Report errors or partial batch results             | `GraphQLException`, `ReportError`, domain errors, or `ResolverResult` | [Result Handling](./resolver-result-handling)                                                                              |
| Add reusable cross-cutting field behavior          | Field middleware                                                      | [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)                                         |

---

## Resolver parameters overview

Resolver method parameters are either GraphQL arguments or values injected by Hot Chocolate. The table below shows each kind:

| Parameter kind         | Example                                        | Appears in schema?  | Detail                                                       |
| ---------------------- | ---------------------------------------------- | ------------------- | ------------------------------------------------------------ |
| GraphQL argument       | `string search`, `int first`                   | Yes                 | [Parameter Attributes](./parameter-attributes)               |
| Typed ID argument      | `[ID<Product>] Guid id`                        | Yes, as `ID` scalar | [Parameter Attributes](./parameter-attributes)               |
| Parent value           | `[Parent] Product product`                     | No                  | [Parent access](./parent-attribute)                          |
| Implicit service       | `ProductService products` (registered in DI)   | No                  | [Service Injection](./service-injection)                     |
| Keyed service          | `[Service("catalog")] ProductService products` | No                  | [Service Injection](./service-injection)                     |
| DataLoader             | `IBrandByIdDataLoader brandById`               | No                  | [DataLoader](/docs/hotchocolate/v16/build/dataloader)        |
| Global state           | `[GlobalState("TenantId")] string tenantId`    | No                  | [HTTP Context and State](./ihttpcontextaccessor-and-context) |
| Resolver context       | `IResolverContext context`                     | No                  | [Resolver Signature](./resolver-signature)                   |
| Cancellation           | `CancellationToken ct`                         | No                  | [Resolver Signature](./resolver-signature)                   |
| Selection optimization | `[IsSelected("address")] bool includeAddress`  | No                  | [Parameter Attributes](./parameter-attributes)               |

> **Watch out:** A parameter is treated as a GraphQL argument unless Hot Chocolate recognizes it as a service or a known special type. If a service parameter appears as a schema argument, make sure the service type is registered in the DI container. See [Service Injection](./service-injection) for more details.

---

## Keeping resolvers focused

A resolver should have a single responsibility: gather its inputs and delegate to the layer that owns the business logic.

**What belongs in a resolver:**

- Reading GraphQL arguments and parent values
- Calling a service, query handler, repository, EF Core query, or DataLoader
- Passing `CancellationToken` to async operations
- Mapping application results to GraphQL values or error responses

**What does not belong in a resolver:**

- Multi-step business workflows
- Repeated per-item database access for nested fields
- Request initialization or session setup
- Long-lived mutable state
- HTTP-specific logic for fields that should work across transports

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

Most data-fetching resolvers return `Task<T>`. Add a `CancellationToken` parameter so Hot Chocolate can cancel the operation if the client disconnects or the request times out. Pass the token to every async call, including EF Core queries, HTTP clients, service calls, and DataLoader `LoadAsync`.

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

**Service scoping behavior:**

- During query execution, services are resolver-scoped by default. Each resolver receives its own scope, allowing sibling fields to run in parallel safely.
- During mutation execution, services are request-scoped by default. All resolvers in the mutation share a single scope, preserving transactional semantics.

For more on scoping, `[UseRequestScope]`, and keyed services, see [Service Injection](./service-injection).

---

## Avoiding N+1 and over-fetching

Nested resolvers can cause N+1 problems when each resolver triggers a separate database query for every parent item. To solve this, batch lookups with a DataLoader.

### Defining a DataLoader

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

### Using the DataLoader in a resolver

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

The DataLoader collects all brand keys from the concurrent resolver wave and then issues a single `WHERE id IN (...)` query. For full DataLoader documentation, see [DataLoader](/docs/hotchocolate/v16/build/dataloader).

**Other strategies to consider:**

- Use paging, filtering, sorting, and projections for collection fields backed by query providers. These features push filtering and shaping to the database layer.
- Use `[BatchResolver]` or `.ResolveBatch(...)` for one-off, field-specific batch operations that do not require a shared DataLoader.
- Use `[IsSelected]` as an advanced optimization to skip expensive operations when the client has not selected a field.

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

You can then read the value in any resolver using `[GlobalState]`:

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

For setup instructions, interceptor configuration, and `IResolverContext` APIs, see [HTTP Context and State](./ihttpcontextaccessor-and-context).

---

## Results and errors overview

| Situation                   | Recommended approach                                                              |
| --------------------------- | --------------------------------------------------------------------------------- |
| Successful field value      | Return the value from the resolver                                                |
| Field returns nothing       | Return `null` for a nullable field; throw or return an error for a non-null field |
| Field-level GraphQL error   | Throw `GraphQLException` or call `IResolverContext.ReportError`                   |
| Expected business failure   | Use mutation payload types with a domain error union field                        |
| Partial batch result errors | Use `ResolverResult` with per-item error entries                                  |

A field error results in a `null` field and an entry in the top-level `errors` array:

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

For details on null propagation, `IError` construction, `ResolverResult`, and mutation error conventions, see [Result Handling](./resolver-result-handling).

---

## Field middleware and resolvers

Field middleware wraps field resolution with reusable behaviors such as authorization checks, caching, instrumentation, or input validation. A resolver provides a value, while middleware can decide whether to call the resolver, inspect its result, or replace it.

`IMiddlewareContext` extends `IResolverContext`, so middleware has access to the same context. Middleware can read and write the `Result` property before or after the resolver runs.

Use middleware for cross-cutting concerns, not for ordinary data loading. Mixing data access into middleware ties it to a specific field's shape and makes reuse more difficult.

For information on pipeline order and `UseField` registration, see [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware).

---

## Troubleshooting

| Symptom                                                   | Likely cause                                              | Fix                                                               | Detail                                                                         |
| --------------------------------------------------------- | --------------------------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| A service parameter appears as a GraphQL argument         | Service type is not registered in DI                      | Register the service or use `[Service("key")]` for keyed services | [Service Injection](./service-injection)                                       |
| Nested field fires many database queries                  | N+1 resolver pattern                                      | Use DataLoader or a batch resolver                                | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                          |
| Query resolver has threading issues with a scoped service | Query fields run concurrently                             | Confirm resolver-scoped service defaults and DI scope overrides   | [Service Injection](./service-injection)                                       |
| Resolver cannot read tenant, user, or correlation data    | State was not set before execution                        | Use an interceptor to write global state before the request runs  | [HTTP Context and State](./ihttpcontextaccessor-and-context)                   |
| Mutation side effects run in an unexpected order          | Only top-level mutation fields are serialized             | Keep side effects in top-level mutation resolvers                 | [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations) |
| Client receives `Unexpected Execution Error`              | Unhandled exception not translated into a GraphQL error   | Use `GraphQLException`, `ReportError`, or domain payload errors   | [Result Handling](./resolver-result-handling)                                  |
| Batch resolver result maps to wrong parent items          | Result list count or order does not match the parent list | Preserve order and count, or use the dictionary keyed pattern     | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                          |

---

## Where to go next

| Goal                                                                                      | Page                                                                                                                                                                                                                                                                                                   |
| ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Understand supported return types, naming rules, and the full method signature            | [Resolver Signature](./resolver-signature)                                                                                                                                                                                                                                                             |
| Access parent values in nested field resolvers                                            | [Parent access](./parent-attribute)                                                                                                                                                                                                                                                                    |
| Inject services and understand resolver versus request scope                              | [Service Injection](./service-injection)                                                                                                                                                                                                                                                               |
| Use argument and parameter attributes such as `[ID]`, `[GraphQLName]`, and `[IsSelected]` | [Parameter Attributes](./parameter-attributes)                                                                                                                                                                                                                                                         |
| Access request state, `IResolverContext`, and HTTP-specific data                          | [HTTP Context and State](./ihttpcontextaccessor-and-context)                                                                                                                                                                                                                                           |
| Handle nulls, errors, and batch result errors                                             | [Result Handling](./resolver-result-handling)                                                                                                                                                                                                                                                          |
| Batch data loading and avoid N+1                                                          | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                                                                                                                                                                                  |
| Add cross-cutting field behavior                                                          | [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware)                                                                                                                                                                                                                     |
| Place fields on Query, Mutation, and object types                                         | [Schema Elements](/docs/hotchocolate/v16/build/schema-elements), [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries), [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations), [Object Types](/docs/hotchocolate/v16/build/schema-elements/object-types) |
