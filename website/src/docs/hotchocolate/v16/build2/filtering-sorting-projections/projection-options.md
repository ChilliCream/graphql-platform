---
title: Projection options
---

Projections let Hot Chocolate shape a data-source query from the GraphQL selection set. The default v16 projection provider builds LINQ `Select` expressions for queryable results. Provider integrations can translate the same selection set into provider-native projection shapes, such as MongoDB projection documents.

Use projections when the resolver returns a supported, unmaterialized source and the data provider can translate the selected members. Do not use projections as authorization, as a replacement for DataLoader, or as a guarantee that every provider will select fewer columns for every request.

# Quick setup

Install `HotChocolate.Data` for queryable projections.

<PackageInstallation packageName="HotChocolate.Data" />

Register projections once with the schema:

```csharp
builder.Services
    .AddGraphQL()
    .AddProjections()
    .AddTypes();
```

Apply projection middleware to a field that returns a queryable source:

```csharp
using HotChocolate;
using HotChocolate.Data;

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

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Brand Brand { get; set; } = default!;
}

public sealed class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
```

The data middleware order is significant:

1. `[UsePaging]` or `.UsePaging()`
2. `[UseProjection]` or `.UseProjection()`
3. `[UseFiltering]` or `.UseFiltering()`
4. `[UseSorting]` or `.UseSorting()`

Use the same order when you configure fields with descriptors:

```csharp
public sealed class ProductQueryResolvers
{
    public IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}

public sealed class ProductQueryType : ObjectType<ProductQueryResolvers>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueryResolvers> descriptor)
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

Invalid order can produce the analyzer or schema validation message `Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]`.

# How projection shapes a query

Given this request:

```graphql
query BrowseProducts {
  products(first: 10) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

The queryable projection provider creates a selected object shape similar to this expression:

```csharp
products.Select(product => new Product
{
    Name = product.Name,
    Brand = new Brand
    {
        Name = product.Brand.Name
    }
});
```

An Entity Framework Core provider can translate that expression into SQL similar to this:

```sql
SELECT "p"."Name", "b"."Id", "b"."Name"
FROM "Products" AS "p"
LEFT JOIN "Brands" AS "b" ON "p"."BrandId" = "b"."Id"
```

The exact SQL depends on the provider, model configuration, selected fields, keys needed for materialization, joins, and optimizers.

Projection works best when the resolver keeps the query unmaterialized:

```csharp
[UseProjection]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}
```

Avoid executing the query before Hot Chocolate applies middleware:

```csharp
[UseProjection]
public static IEnumerable<Product> GetProducts(CatalogContext db)
{
    return db.Products.ToList();
}
```

Returning a materialized list means the database query has already run. The default provider can still apply LINQ over `IEnumerable<T>`, but database pushdown has already been lost.

# Registration options

There is no `ProjectionOptions` options bag in v16. Projection customization is based on conventions, providers, scopes, field middleware, and field metadata.

| Scenario                        | Registration API                                                | Resolver result shape                                                    | Field API                                                 | Notes                                                                   |
| ------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------ | --------------------------------------------------------- | ----------------------------------------------------------------------- |
| Default queryable projections   | `.AddProjections()`                                             | `IQueryable<T>`, `IEnumerable<T>`, `IQueryableExecutable<T>`             | `[UseProjection]`, `.UseProjection()`                     | Database optimization requires an unmaterialized provider query.        |
| Named queryable convention      | `.AddProjections("sql")`                                        | Same as default queryable projections                                    | `[UseProjection(Scope = "sql")]`, `.UseProjection("sql")` | Use scopes when more than one projection convention is registered.      |
| Configured queryable convention | `.AddProjections(x => x.AddDefaults(), name: "sql")`            | Shapes supported by the configured provider                              | `[UseProjection(Scope = "sql")]`                          | Use the convention descriptor for provider and handler customization.   |
| Custom convention type          | `.AddProjections<MyProjectionConvention>("custom")`             | Shapes supported by the custom convention                                | `.UseProjection("custom")`                                | The convention must provide a projection provider.                      |
| MongoDB                         | `.AddMongoDbProjections()` or `.AddMongoDbProjections("mongo")` | MongoDB executable results, such as `IMongoCollection<T>.AsExecutable()` | `[UseProjection]` or `[UseProjection(Scope = "mongo")]`   | Use a scope when MongoDB and queryable projections are both registered. |
| RavenDB                         | `.AddRavenProjections()`                                        | Raven query shapes supported by the Raven provider                       | `[UseProjection]`                                         | Keep provider-specific behavior in the integration layer.               |
| Spatial                         | `.AddSpatialProjections()`                                      | Queryable shapes with spatial members                                    | `[UseProjection]`                                         | Adds spatial projection handlers as a provider extension.               |

MongoDB projections can produce a document like this for a selection of `name` and `addresses.city`:

```json
{
  "find": "person",
  "filter": {},
  "projection": { "Addresses.City": 1, "Name": 1 }
}
```

MongoDB projections can reduce transferred data, but they can also be a performance tradeoff. Measure the query plan for the workload.

# Field-level configuration

| API                                           | Where used                             | Effect                                                           | Common recipe                                                               | Caveats                                                                            |
| --------------------------------------------- | -------------------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `[UseProjection]`                             | Resolver method or property            | Adds projection middleware to the field                          | Return `IQueryable<T>` and keep the query unmaterialized                    | Must be ordered after paging and before filtering and sorting.                     |
| `[UseProjection(Scope = "name")]`             | Resolver method or property            | Selects a named projection convention                            | Use provider scopes, for example MongoDB beside queryable projections       | Align filtering and sorting scopes when those provider conventions are scoped too. |
| `.UseProjection()`                            | Object field descriptor                | Adds projection middleware with inferred element type            | Use in `ObjectType<T>` configuration                                        | Throws during schema completion if the result type cannot be inferred.             |
| `.UseProjection<T>()`                         | Object field descriptor                | Adds projection middleware with an explicit projected type       | Use when inference is not enough                                            | The type must match the result shape the provider receives.                        |
| `.UseProjection(typeof(T), scope)`            | Object field descriptor                | Adds projection middleware with runtime type and optional scope  | Use from shared descriptor helpers                                          | Pass the scope registered with `.AddProjections(name)` or a provider integration.  |
| `[IsProjected]`, `[IsProjected(true)]`        | Object field member or resolver method | Always includes that field in projection metadata                | Keep a key or leaf dependency available for another resolver                | Best for simple leaf fields.                                                       |
| `.IsProjected()`                              | Object field descriptor                | Fluent equivalent of `[IsProjected]`                             | Use in type configuration                                                   | Same effect as `.IsProjected(true)`.                                               |
| `[IsProjected(false)]`, `.IsProjected(false)` | Object field member or descriptor      | Prevents that field from being included in generated projections | Exclude service-backed or computed fields from database selection           | The field stays in the GraphQL schema.                                             |
| `[UseFirstOrDefault]` with `[UseProjection]`  | Resolver returning `IQueryable<T>`     | Exposes one nullable item while middleware still sees a sequence | Query by id and return `IQueryable<T>`                                      | Place `[UseFirstOrDefault]` above `[UseProjection]`.                               |
| `[UseSingleOrDefault]` with `[UseProjection]` | Resolver returning `IQueryable<T>`     | Exposes one nullable item with single-result semantics           | Use when more than one match should be an error in the underlying operation | Keep projection in the same data middleware order.                                 |

`[UseProjection]` exposes `Scope` and the inherited descriptor attribute `Order`. It does not expose an attribute parameter for an explicit projection type. Use descriptor configuration when you need `.UseProjection<T>()` or `.UseProjection(typeof(T), scope)`.

# Include fields clients did not request

Resolvers often need a key that is not part of the client selection. Use `[IsProjected]` for a leaf member that should always be available in projected parent objects.

```csharp
public sealed class Product
{
    [IsProjected]
    public int Id { get; set; }

    public int BrandId { get; set; }

    public string Name { get; set; } = string.Empty;
}
```

The fluent equivalent is:

```csharp
descriptor
    .Field(t => t.Id)
    .IsProjected();
```

For source-generated parent resolver fields, declare the parent member dependency where the parent value is read:

```csharp
[ObjectType<Product>]
public static partial class ProductResolvers
{
    [BindMember(nameof(Product.BrandId))]
    public static Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken ct)
    {
        return brandById.LoadAsync(product.BrandId, ct);
    }
}
```

`[Parent(requires: ...)]` records that the resolver needs the parent member. Keep the required member available on the object returned by the data layer, and verify the provider path used by the field. When a field is always needed by multiple resolvers, `[IsProjected]` or `.IsProjected()` can be a clearer local rule.

# Exclude fields from generated projections

Use `[IsProjected(false)]` to keep a GraphQL field out of generated projection selection.

```csharp
public sealed class Product
{
    public string Name { get; set; } = string.Empty;

    [IsProjected(false)]
    public string InternalNotes { get; set; } = string.Empty;
}
```

The fluent equivalent is:

```csharp
descriptor
    .Field(t => t.InternalNotes)
    .IsProjected(false);
```

This does not remove the field from the schema. If a client selects `internalNotes`, the field still needs a value. Provide a resolver, return a value that already contains the member, or expect the CLR default value when the projection did not bind it.

Do not use projection exclusion as a security boundary. Use authorization, schema design, or separate models for sensitive data.

# Entity shape requirements

The default queryable provider builds new object shapes by binding CLR members. Public getters expose fields in GraphQL, but default queryable projection binding needs writable members for values it materializes.

```csharp
public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Brand Brand { get; set; } = default!;
}
```

A member without a writable property can be skipped by the projection and can later resolve as a default value. Provider-specific construction can differ, so verify behavior with the provider you register.

Fields with custom resolver logic are not translated into the parent database projection. Use one of these patterns instead:

- Mark simple parent dependencies with `[IsProjected]`, `.IsProjected()`, or `[Parent(requires: ...)]` where supported.
- Resolve external or service-backed fields with DataLoader or batching.
- Use an explicit DTO query when the database projection must be fully controlled by application code.
- Move layered query shaping to `QueryContext<T>` when the service layer owns projection, filtering, and sorting.

# Nested projections and relationships

Nested object selections can be projected through navigation members when the member can be bound and the provider translates the shape.

```graphql
query {
  products {
    name
    brand {
      name
    }
  }
}
```

For queryable projections, that selection becomes a nested object initializer in a LINQ `Select`. Nested list selections can become nested `Select` expressions as well, but provider translation controls what reaches the database.

A nested field that also has `.UseProjection()` or `[UseProjection]` is a separate projection boundary. Projection is not nested paging. When a relationship needs its own paging arguments, model it as its own field with paging middleware or use a service-layer connection pattern.

# Single-item fields

Use `[UseFirstOrDefault]` or `[UseSingleOrDefault]` when the resolver should expose one item while still returning `IQueryable<T>` to the middleware pipeline.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UseFirstOrDefault]
    [UseProjection]
    public static IQueryable<Product> GetProductById(int id, CatalogContext db)
    {
        return db.Products.Where(t => t.Id == id);
    }
}
```

The schema exposes a single nullable product:

```graphql
type Query {
  productById(id: Int!): Product
}
```

Use `[UseSingleOrDefault]` when more than one matching row should be treated as an error by the underlying query operation. Descriptor equivalents are `.UseFirstOrDefault()` and `.UseSingleOrDefault()`.

# QueryContext<T> as an alternative

`QueryContext<T>` is a separate v16 pattern for layered applications. It carries projection, filtering, and sorting information to an application service instead of applying projection middleware to an `IQueryable<T>` field.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static QueryContext<Product> GetProducts(CatalogContext db)
    {
        return db.Products.AsQueryContext();
    }
}
```

Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. The `HC0099` analyzer reports that combination because both paths try to own projection handling.

Use `QueryContext<T>` when the service layer must compose the query. Use `[UseProjection]` when the field can return a provider-supported source directly.

# Troubleshooting

| Symptom                                             | Likely cause                                                                                                                   | How to confirm                                    | Fix                                                                                         |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Data attribute order error                          | Middleware is not in the supported order                                                                                       | Check the resolver attributes or descriptor chain | Use paging, projection, filtering, sorting.                                                 |
| `HC0099` analyzer warning                           | `QueryContext<T>` is combined with projection middleware                                                                       | Check the field return type and attributes        | Remove `[UseProjection]` or stop using `QueryContext<T>` for that field.                    |
| Field resolves as default value or null             | No writable member, the field was excluded, or the provider could not bind it                                                  | Inspect the CLR member and projection metadata    | Add a writable member, remove `[IsProjected(false)]`, or provide a resolver.                |
| Resolver needs a key that the client did not select | The key is not part of the selection set                                                                                       | Inspect generated SQL or provider projection      | Use `[IsProjected]`, `.IsProjected()`, or `[Parent(requires: ...)]` where supported.        |
| SQL selects more columns than expected              | Provider needs keys, query was materialized early, provider cannot translate the exact shape, or model configuration adds data | Enable provider SQL logging                       | Return an unmaterialized query and measure provider output.                                 |
| MongoDB projection is not applied                   | Missing MongoDB projection registration, wrong scope, or non-Mongo result shape                                                | Check registration and resolver return type       | Register `.AddMongoDbProjections()` and match `[UseProjection(Scope = "...")]` when scoped. |
| Custom resolver work is not in SQL                  | Projection does not translate arbitrary resolver logic                                                                         | Inspect the field definition                      | Use DataLoader, batching, parent requirements, or explicit DTO queries.                     |
| Nested paging is not projected                      | Projection is not paging middleware for child connections                                                                      | Check the nested field definition                 | Add nested paging middleware or use a service-layer connection.                             |

# When to use DTOs or DataLoader instead

Choose explicit DTO resolvers when the selected database shape must differ from the GraphQL runtime type, when constructor-only values are required, or when the provider cannot translate the generated object initializer.

Choose DataLoader when the field resolves through another service, requires batching by key, or would otherwise cause additional per-row database calls. Projection can keep the key available on the parent object, and DataLoader can fetch the related values efficiently.

# Next steps

- Review the data middleware overview in [Filtering, sorting, and projections](/docs/hotchocolate/v16/build2/filtering-sorting-projections).
- Configure fields with [UseProjection](/docs/hotchocolate/v16/build2/attributes/useprojection).
- Learn parent resolver requirements in [Parent access](/docs/hotchocolate/v16/build2/resolvers/parent-attribute).
- Configure provider details in [Entity Framework Integration](/docs/hotchocolate/v16/integrations/entity-framework) and [MongoDB Integration](/docs/hotchocolate/v16/integrations/mongodb).
