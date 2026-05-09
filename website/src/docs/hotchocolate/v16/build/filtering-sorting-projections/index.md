---
title: Filtering, sorting, and projections
---

Filtering, sorting, and projections are data middleware in Hot Chocolate designed for collection fields. These features allow a GraphQL request to specify page size, selected fields, predicates, and order, while keeping your resolver focused on returning a composable source.

Use these middleware together when exposing database-backed data. The provider must be able to interpret the resolver result, GraphQL arguments, and selection set before materializing the result.

# What each middleware provides

| Feature     | GraphQL input                                           | Server effect                      | Common provider output                                       |
| ----------- | ------------------------------------------------------- | ---------------------------------- | ------------------------------------------------------------ |
| Paging      | `first`, `after`, `last`, `before`, or offset arguments | Bounds and shapes the collection   | Provider paging operation and a connection or segment        |
| Projections | Selection set                                           | Selects the requested object shape | LINQ `Select(...)`, MongoDB projection, or provider selector |
| Filtering   | `where`                                                 | Applies predicates                 | LINQ `Where(...)`, MongoDB filter, or provider predicate     |
| Sorting     | `order`                                                 | Applies ordering                   | LINQ `OrderBy(...)`, MongoDB sort, or provider sort          |

The default configuration is a productive starting point. For public APIs, review every exposed filter and sort field for authorization, indexes, and cost limits.

# Mental model

Resolvers should return an unmaterialized source. Hot Chocolate reads the request, passes the source through the data middleware, and asks the active provider to translate the operations.

```text
GraphQL request
  selection set + where + order + paging arguments
        |
Configured data middleware
  UsePaging -> UseProjection -> UseFiltering -> UseSorting
        |
Provider translation
  IQueryable expressions / MongoDB documents / Marten-compatible LINQ
        |
Database query, provider execution, or intentional in-memory processing
```

If the resolver returns data that is already loaded, or if the provider cannot translate a requested operation, the work may run in memory or fail. Keep large collections as `IQueryable<T>` or a provider-specific executable until middleware has completed.

# Building a database-backed field

Install `HotChocolate.Data` to enable filtering, sorting, and projections.

<PackageInstallation packageName="HotChocolate.Data" />

Register these features once when configuring the schema:

```csharp
builder.Services
    .AddDbContext<CatalogContext>()
    .AddGraphQL()
    .AddTypes()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

Expose the collection by returning `IQueryable<Product>`:

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}

public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public decimal Price { get; set; }

    public Brand Brand { get; set; } = default!;
}

public sealed class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
```

Expected SDL shape:

```graphql
type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
    where: ProductFilterInput
    order: [ProductSortInput!]
  ): ProductsConnection
}

type ProductsConnection {
  pageInfo: PageInfo!
  edges: [ProductsEdge!]
  nodes: [Product!]
}

type ProductsEdge {
  cursor: String!
  node: Product!
}

type Product {
  id: Int!
  name: String!
  price: Decimal!
  brand: Brand!
}

input ProductFilterInput {
  and: [ProductFilterInput!]
  or: [ProductFilterInput!]
  name: StringOperationFilterInput
  price: DecimalOperationFilterInput
  brand: BrandFilterInput
}

input ProductSortInput {
  name: SortEnumType
  price: SortEnumType
  id: SortEnumType
}
```

Example client query:

```graphql
query BrowseProducts($after: String) {
  products(
    first: 20
    after: $after
    where: { brand: { name: { eq: "Contoso" } }, price: { lte: 50 } }
    order: [{ name: ASC }, { id: ASC }]
  ) {
    nodes {
      id
      name
      price
      brand {
        name
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

Use the `endCursor` value as `after` for the next page. If `where` or `order` changes, restart paging from the beginning, as the logical result set has changed.

A representative SQL query for the first request, when EF Core can translate the full pipeline, might look like this:

```sql
SELECT "p"."Id", "p"."Name", "p"."Price", "b"."Id", "b"."Name"
FROM "Products" AS "p"
LEFT JOIN "Brands" AS "b" ON "p"."BrandId" = "b"."Id"
WHERE "b"."Name" = 'Contoso' AND "p"."Price" <= 50.0
ORDER BY "p"."Name", "p"."Id"
LIMIT 20
```

The exact output depends on the database provider, mappings, selected fields, and paging provider. The key point is that the provider receives a single composed query while the source remains unmaterialized.

# Middleware order

Declare data middleware in this order, for both attributes and fluent descriptors:

1. `[UsePaging]` or `.UsePaging()`
2. `[UseProjection]` or `.UseProjection()`
3. `[UseFiltering]` or `.UseFiltering()`
4. `[UseSorting]` or `.UseSorting()`

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}
```

```csharp
public sealed class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UsePaging()
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
}
```

Middleware is invoked in the order declared, and the resolver result flows back through the pipeline in reverse. Sorting and filtering compose the logical source, projection selects the requested shape, and paging shapes the final connection. For more on the execution model, see [field middleware](../execution-engine/field-middleware).

# Attribute and descriptor entry points

Attributes keep local field rules close to the resolver. Descriptor APIs are better suited when schema configuration is managed in type classes, shared helpers, or modules.

```csharp
[UsePaging(MaxPageSize = 100)]
[UseProjection]
[UseFiltering(typeof(ProductFilterType))]
[UseSorting(typeof(ProductSortType))]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}
```

```csharp
descriptor
    .Field(t => t.GetProducts(default!))
    .UsePaging(options => options.MaxPageSize = 100)
    .UseProjection()
    .UseFiltering<ProductFilterType>()
    .UseSorting<ProductSortType>();
```

For detailed attribute guidance, see [UsePaging](../attributes/usepaging), [UseProjection](../attributes/useprojection), [UseFiltering](../attributes/usefiltering), and [UseSorting](../attributes/usesorting).

# Designing the schema surface

Inferred filter and sort inputs expose fields from the runtime type according to the active conventions. This is useful for prototypes and trusted internal APIs. Public APIs typically require explicit inputs:

```csharp
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

public sealed class ProductFilterType : FilterInputType<Product>
{
    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.Brand);
        descriptor.Field(p => p.Price);
    }
}

public sealed class ProductSortType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.Price);
        descriptor.Field(p => p.Id);
    }
}
```

Explicit types allow you to include indexed fields, remove sensitive properties, avoid unsupported provider operations, and add a stable tie-breaker such as `id` for paged sorting. For paging size and null-order behavior, see [paging options](../pagination/paging-options).

Projection also has schema design constraints. Default projections require public setters for projected members. Custom resolver code is not translated into SQL or MongoDB projections. If a resolver needs a value that the client did not select, mark that member with `IsProjected(true)` in the object type configuration.

# Provider and data access choices

| Situation                      | Resolver shape                                                                 | Notes                                                                                                                                                         |
| ------------------------------ | ------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EF Core or LINQ provider       | Return `IQueryable<T>`                                                         | The default providers build expression trees. The LINQ provider determines what can be translated to SQL or another backend.                                  |
| MongoDB                        | Return `IExecutable<T>` from `AsExecutable()`                                  | Register MongoDB filtering, sorting, projection, and paging providers as needed. Use matching scopes when MongoDB and LINQ providers are registered together. |
| Marten                         | Return Marten `IQueryable<T>`                                                  | Register `.AddMartenFiltering()` and `.AddMartenSorting()`. Marten projections and paging work through the documented integration.                            |
| In-memory data                 | Return `IEnumerable<T>` intentionally                                          | Filtering and sorting can run after the collection is loaded. Avoid this for large public collections unless that cost is planned.                            |
| Service layer owns query rules | Accept explicit inputs, paging arguments, or `GreenDonut.Data.QueryContext<T>` | Keeps tenant filters, authorization, and deterministic ordering in application code.                                                                          |
| Related child data             | Use [DataLoader](../dataloader/)                                               | DataLoader batches key-based lookups. It does not replace collection filtering, sorting, or projections.                                                      |

`GreenDonut.Data.QueryContext<T>` is a resolver parameter and service-layer bridge. Hot Chocolate can build it from the field selection, filter context, and sort context, allowing application code to apply those parts to its own query model. It is not a resolver return type. Do not combine a `QueryContext<T>` parameter with `[UseProjection]` on the same field.

# Troubleshooting

| Problem                                              | Likely cause                                                                       | Fix                                                                                                                 |
| ---------------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| Analyzer or schema error about data middleware order | Attributes or fluent calls are out of order                                        | Reorder to paging, projection, filtering, sorting.                                                                  |
| Filtering or sorting runs in memory                  | The resolver materialized the collection or returned an unsupported source         | Return `IQueryable<T>`, a provider-specific `IExecutable<T>`, or move the work into the service layer deliberately. |
| Projected members have default values                | A projected member lacks a public setter or cannot be materialized by the provider | Add a public setter, configure the object type, or resolve the field outside the projection.                        |
| A resolver needs a field the client did not select   | Projection omitted a value required by server code                                 | Mark the member with `IsProjected(true)` or redesign the resolver.                                                  |
| A custom resolver is not pushed into the database    | Projections do not translate arbitrary resolver code                               | Keep projected fields as model members or use provider-specific query code.                                         |
| Paged results repeat or shift                        | The sort order is not deterministic                                                | Add a unique tie-breaker and review [pagination](../pagination/).                                                   |
| MongoDB operations do not apply                      | MongoDB providers or scopes are missing                                            | Register the MongoDB providers and use matching `Scope` values on data attributes.                                  |
| Marten filtering or sorting fails                    | Marten-specific conventions are missing                                            | Register `.AddMartenFiltering()` and `.AddMartenSorting()`.                                                         |
| `QueryContext<T>` reports a projection conflict      | `[UseProjection]` is on a field that accepts `QueryContext<T>`                     | Use projection middleware or `QueryContext<T>`, not both on the same field.                                         |
| Public API exposes too many fields                   | Inferred filters or sorts expose unwanted members                                  | Use explicit filter and sort types with `BindFieldsExplicitly()`.                                                   |

# Next steps

- Start with the focused feature guides for [filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), [sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types), and [projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).
- Design the schema surface with [filter types](filter-types.md) and [sort types](sort-types.md).
- Configure pagination with [pagination](../pagination/) and [paging options](../pagination/paging-options).
- Use attributes with [UseFiltering](../attributes/usefiltering), [UseSorting](../attributes/usesorting), [UseProjection](../attributes/useprojection), and [UsePaging](../attributes/usepaging).
- Use service-layer and child-field patterns with [DataLoader](../dataloader/).
- Learn middleware ordering in [field middleware](../execution-engine/field-middleware).
- Extend provider behavior with [extending filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering).
