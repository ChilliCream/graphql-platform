---
title: UseSorting attribute
---

A collection field is more useful when clients can choose a predictable order. `[UseSorting]` is the code-first attribute that adds sorting middleware and an `order` argument to any resolver method or property that returns a collection. The middleware intercepts the resolver result and applies the requested sort before the response is sent.

# Add sorting to a collection field

**Prerequisites:**

- Add the `HotChocolate.Data` package to your project.
- Register the sorting provider on the schema builder.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddSorting();
```

Apply `[UseSorting]` to the resolver method or property that exposes the collection:

```csharp
#nullable enable

using HotChocolate.Data;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

The resolver returns an `IQueryable<Product>`. The middleware composes a sort expression and passes the rewritten query to the database. No extra resolver code is required.

Client query:

```graphql
query {
  products(order: [{ name: ASC }]) {
    name
    price
  }
}
```

# What the attribute adds to the schema

`[UseSorting]` generates several schema elements automatically.

**Field argument.** The attribute adds an `order` argument to the field. The name comes from the active `SortConvention`; the default is `order`. If you configure a convention with a different argument name, the generated argument reflects that name.

**Sort input type.** Hot Chocolate infers a `<TypeName>SortInput` input type from the model. Each sortable scalar property becomes a field of type `SortEnumType`.

**Sort enum.** `SortEnumType` exposes `ASC` and `DESC` values.

**List argument.** The argument type is a list of non-null sort input objects, for example `[ProductSortInput!]`. Clients pass multiple conditions, and the provider applies them in order.

Example SDL for the resolver above:

```graphql
type Query {
  products(order: [ProductSortInput!]): [Product!]!
}

input ProductSortInput {
  name: SortEnumType
  price: SortEnumType
}

enum SortEnumType {
  ASC
  DESC
}
```

Multi-field client query:

```graphql
products(order: [{ name: ASC }, { id: ASC }]) {
  name
  price
}
```

For nested objects, sorting extends to related object properties. See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for nested sort inputs, multi-field sorting, custom enum types, and convention options.

# Put `[UseSorting]` in the data middleware stack

When you combine paging, projections, filtering, and sorting on the same field, place the attributes in this top-to-bottom order:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products;
```

The attributes compose field middleware. The top attribute wraps the outermost layer and the bottom attribute wraps the innermost layer. That means `UsePaging` runs first and receives the already-filtered, already-sorted queryable from the layers below it.

Hot Chocolate includes a Roslyn analyzer that detects incorrect order on resolver methods and offers a code fix. The analyzer covers method declarations. If you use properties, verify the order manually.

Full example with all four middleware attributes:

```csharp
#nullable enable

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
        => db.Products;
}
```

See [UsePaging attribute](usepaging), [UseProjection attribute](useprojection), and [UseFiltering attribute](usefiltering) for details on those layers.

# Choose the collection source deliberately

The shape of the resolver return type determines how the middleware applies sorting.

| Resolver result shape                        | Use when                                                           | Behavior                                                                                                                                                              |
| -------------------------------------------- | ------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IQueryable<T>`                              | EF Core or another LINQ provider owns the query                    | The default provider composes expression trees. The database receives the sort as part of the translated query. This is the preferred path for database-backed lists. |
| `IEnumerable<T>`                             | Data is already small and in memory                                | Supported. The middleware converts the sequence to a queryable and sorts it in memory after the resolver runs. Do not use this for large database tables.             |
| `IQueryableExecutable<T>`                    | The source is wrapped by Hot Chocolate's queryable executable path | The default provider rewrites the executable source before execution.                                                                                                 |
| `IExecutable<T>` with a provider integration | MongoDB and similar integrations                                   | Register the provider-specific sorting convention and use `Scope` when mixing providers. See [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb).      |
| `Connection<T>` with `QueryContext<T>`       | A service layer owns paging, filtering, and sorting                | Capture sort definitions in the service layer and apply them during execution.                                                                                        |

See [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases) for database-backed `IQueryable<T>` resolver patterns and [Executable](/docs/hotchocolate/v16/api-reference/executable) for the executable abstraction.

# Limit or reshape sortable fields with a sort input type

By default, Hot Chocolate infers a sort input from every public property on the model. That can expose internal fields or allow sorting on computed properties that your database cannot translate. Use an explicit `SortInputType<T>` to control which fields appear.

```csharp
#nullable enable

using HotChocolate.Data.Sorting;

public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.Price);
    }
}
```

`BindFieldsExplicitly()` removes all inferred fields so only the listed fields appear in the generated input type.

Apply the sort input type using any of the three attribute forms:

```csharp
// Constructor form
[UseSorting(typeof(ProductSortInputType))]
public static IQueryable<Product> GetProducts(CatalogContext db) => db.Products;

// Property initializer form
[UseSorting(Type = typeof(ProductSortInputType))]
public static IQueryable<Product> GetProducts(CatalogContext db) => db.Products;

// Generic form (concise when the sort type is known at compile time)
[UseSorting<ProductSortInputType>]
public static IQueryable<Product> GetProducts(CatalogContext db) => db.Products;
```

All three forms produce the same schema result. The generic form `[UseSorting<T>]` is a sealed convenience attribute that passes `typeof(T)` to the base attribute.

Generated SDL with the explicit sort type:

```graphql
type Query {
  products(order: [ProductSortInput!]): [Product!]!
}

input ProductSortInput {
  name: SortEnumType
  price: SortEnumType
}
```

For nested object sorting, add a nested sort input type and reference it:

```csharp
public sealed class ProductSortInputType : SortInputType<Product>
{
    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.Price);
        descriptor.Field(p => p.Brand).Type<BrandSortInputType>();
    }
}

public sealed class BrandSortInputType : SortInputType<Brand>
{
    protected override void Configure(ISortInputTypeDescriptor<Brand> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(b => b.Name);
    }
}
```

See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for custom enum bindings and convention-wide configuration.

# Use a named convention or provider scope

`Scope` selects which sorting convention and provider to use for a field. It is a convention registration name, not a GraphQL field or argument name.

Use a named convention when one group of fields needs different sort operation names, argument names, handlers, or defaults:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddConvention<ISortConvention, CatalogSortConvention>("Catalog")
    .AddSorting();
```

```csharp
using HotChocolate.Data;
using HotChocolate.Data.Sorting;

public sealed class CatalogSortConvention : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.Operation(DefaultSortOperations.Ascending).Name("ASCENDING");
        descriptor.Operation(DefaultSortOperations.Descending).Name("DESCENDING");
    }
}
```

Apply the matching scope to fields that should use that convention:

```csharp
[UseSorting(Scope = "Catalog")]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products;
```

The default provider handles `IQueryable<T>` and `IEnumerable<T>`. When you add a second provider, such as MongoDB, register it under a named scope so the two providers do not conflict:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddSorting()                                          // default provider, no scope
    .AddMongoDbSorting("Mongo");                           // MongoDB provider, "Mongo" scope
```

Apply the matching scope on fields that use the MongoDB provider:

```csharp
[UseSorting(Scope = "Mongo")]
public static IExecutable<Product> GetProducts(IMongoCollection<Product> collection)
    => collection.AsExecutable();
```

Fields without `Scope` use the default provider. Fields with `Scope = "Catalog"` use the named convention. Fields with `Scope = "Mongo"` use the MongoDB provider.

Scoped conventions can affect generated type names and enum value names. For example, a convention named `"Bar"` produces `Bar_FooSortInput` and `Bar_SortEnumType`. See [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb) for provider setup and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for sort convention configuration.

# Add default or stable ordering

Cursor-based pagination requires a deterministic sort order to produce correct page boundaries. Client-provided sort fields can contain ties. For example, many products can share the same `name`. Without a unique tie-breaker, the database may return rows in a different order on each page request, causing duplicates or gaps.

If you inspect the sort state in the resolver and still want the middleware to apply the client sort, call `Handled(false)`. This pattern is useful for choosing a default order when the client did not provide `order`:

```csharp
#nullable enable

using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(
        CatalogContext db,
        ISortingContext sortingContext)
    {
        sortingContext.Handled(false); // let the middleware still apply client sorting

        return sortingContext.IsDefined
            ? db.Products
            : db.Products.OrderBy(p => p.Id);
    }
}
```

This gives unsorted requests a deterministic order. It does not add a tie-breaker to client-provided sort fields.

When cursor paging must remain stable for every client sort, append the unique key where you execute the sort. In service-layer patterns that use `QueryContext<T>`, carry the sort definition to the service and add the primary key there:

```csharp
using GreenDonut.Data;

public sealed class ProductService(CatalogContext db)
{
    public Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product>? queryContext = null,
        CancellationToken cancellationToken = default)
    {
        queryContext ??= QueryContext<Product>.Empty;

        return db.Products
            .With(queryContext.Order(StableProductSort))
            .ToPageAsync(pagingArgs, cancellationToken);
    }

    private static SortDefinition<Product> StableProductSort(
        SortDefinition<Product> sort)
        => sort.IfEmpty(s => s.AddAscending(p => p.Name))
            .AddAscending(p => p.Id);
}
```

`IfEmpty` supplies a default sort when the client does not pass `order`. `AddAscending(p => p.Id)` always appends the unique tie-breaker. See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for cursor details and null ordering options.

Do not rely on database natural order. It is not guaranteed to be stable across queries.

# Inspect or take over sorting in advanced resolvers

`ISortingContext` gives the resolver access to the sort state without replacing the middleware. It is useful for default sort decisions, layered services, and custom execution.

Calling `GetSortingContext()` or injecting `ISortingContext` can mark sorting as handled depending on the resolver path, which prevents the middleware from applying the client-provided sort automatically. Call `Handled(false)` to re-enable automatic sorting after inspection.

```csharp
[UseSorting]
public static IQueryable<Product> GetProducts(
    CatalogContext db,
    ISortingContext sortingContext)
{
    sortingContext.Handled(false); // re-enable automatic middleware sorting

    if (sortingContext.IsDefined)
    {
        // read which fields the client selected
        var fields = sortingContext.GetFields();
        // use fields for logging, validation, or routing
    }

    return db.Products;
}
```

`AsSortDefinition<T>()` converts the client sort into a `SortDefinition<T>` that you can pass to a service layer for execution outside the middleware.

# Attribute reference

| Item                    | Value                                                                           |
| ----------------------- | ------------------------------------------------------------------------------- |
| Namespace               | `HotChocolate.Data`                                                             |
| Package                 | `HotChocolate.Data`                                                             |
| Base type               | `DescriptorAttribute`                                                           |
| Targets                 | Methods and properties                                                          |
| Applies to descriptors  | Object fields and interface fields                                              |
| Non-generic constructor | `UseSortingAttribute(Type? sortingType = null, int order = caller line number)` |
| Generic attribute       | `UseSortingAttribute<T>` / `[UseSorting<T>]`                                    |
| `Type` property         | Sort input type that specifies the sort object structure                        |
| `Scope` property        | Sorting convention scope name                                                   |
| Default registration    | `.AddSorting()` for the default queryable provider                              |

The non-generic `UseSortingAttribute` is inherited and allows multiple usages on the same member. In practice, placing more than one sorting attribute on the same field is unusual and can produce duplicate middleware or conflicting arguments.

The `order` constructor parameter participates in descriptor attribute ordering. Leave it at its default, which captures the source line number, unless you need explicit ordering of multiple descriptor attributes on the same member.

Object fields receive both sorting middleware and the `order` argument. Interface fields receive the `order` argument for the interface schema surface but do not get sorting middleware (middleware runs on the implementing object type).

# Troubleshooting

| Symptom                                                  | Likely cause                                                                                           | Fix                                                                                                                                                |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| The `order` argument is missing                          | Sorting was not registered, the attribute is on the wrong member, or the field type cannot be inferred | Register `.AddSorting()`, place the attribute on the resolver method or property that exposes the collection, and return a supported source shape. |
| Sorting works but the database loads the full table      | The resolver materializes data before middleware runs                                                  | Return `IQueryable<T>` instead of a preloaded list for large database collections.                                                                 |
| Paging produces duplicate or skipped rows                | Sort order is not deterministic                                                                        | Add a stable unique tie-breaker, commonly `Id`, when using cursor pagination.                                                                      |
| Filtering, projection, or sorting results look wrong     | Data middleware attributes are out of order                                                            | Use `[UsePaging]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]` from top to bottom on the resolver method.                                  |
| Scoped provider does not apply                           | Registration scope and attribute scope do not match                                                    | Verify the named convention or provider registration name matches the value in `[UseSorting(Scope = "...")]`.                                      |
| Custom sort input omits an expected field                | `BindFieldsExplicitly()` was used and the field was not included                                       | Add the field to the sort input type, or remove explicit binding if all fields should be included.                                                 |
| Sorting stops after inspecting the context               | Sorting was marked as handled                                                                          | Call `sortingContext.Handled(false)` when automatic middleware should still execute after inspection.                                              |
| The GraphQL argument is not named `order`                | A sort convention changed the argument name                                                            | Check the `SortConvention.ArgumentName(...)` configuration on the active convention.                                                               |
| Sorting nullable fields gives unexpected page boundaries | Provider null ordering differs from expectations                                                       | See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for `NullOrdering` configuration.                                           |

# Next steps

- [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting): generated sort inputs, nested sorting, multi-field sorting, custom enum types, conventions, and argument names.
- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination): cursor pagination, deterministic ordering, `NullOrdering`, and providers.
- [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections): database pushdown and combined data middleware.
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering): paired data middleware and filter input concepts.
- [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases): returning `IQueryable<T>` for translated database queries.
- [MongoDB integration](/docs/hotchocolate/v16/integrations/mongodb): non-default data provider and scoped sorting conventions.
- [Executable](/docs/hotchocolate/v16/api-reference/executable): the executable abstraction for provider-backed data sources.
- [Attributes](index): descriptor attributes, middleware ordering, and the attribute mental model.
