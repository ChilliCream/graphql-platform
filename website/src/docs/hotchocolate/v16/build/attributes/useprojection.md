---
title: UseProjection attribute
---

The `[UseProjection]` attribute enables Hot Chocolate to build a provider projection based on the GraphQL selection set. For database-backed fields, this can reduce the number of selected columns and joins when the underlying provider supports translating the projection.

This attribute is the code-first entry point for projections in Hot Chocolate. The most common scenario is an Entity Framework Core resolver that returns an unmaterialized `IQueryable<T>`.

# How `[UseProjection]` Works

When a client requests only certain fields, the projection middleware instructs the configured projection provider to shape the source query to match the selection.

```text
GraphQL selection set
        |
        v
[UseProjection] middleware
        |
        v
Configured projection provider
        |
        v
Provider query selects requested fields when translation is possible
```

For example, a client might request only `name` and `brand.name`:

```graphql
query GetProducts {
  products {
    name
    brand {
      name
    }
  }
}
```

If the resolver returns `IQueryable<Product>`, the default queryable projection provider creates a LINQ `Select` expression for the selected subtree. Entity Framework Core or another LINQ provider then determines how to translate that expression.

A representative SQL query for the example above might look like:

```sql
SELECT "p"."Name", "b"."Id", "b"."Name"
FROM "Products" AS "p"
LEFT JOIN "Brands" AS "b" ON "p"."BrandId" = "b"."Id"
```

The actual query depends on the provider, model mapping, selected fields, and query plan.

# Enabling Projections

Projections are included in the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register projections when configuring your schema:

```csharp
builder.Services
    .AddGraphQL()
    .AddProjections()
    .AddTypes();
```

Import the data namespace in files where you use the attribute:

```csharp
using HotChocolate.Data;
```

# Adding Projection to a Query Field

Return the queryable source from your resolver and apply `[UseProjection]` to the field.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Data;

[QueryType]
public static partial class ProductQueries
{
    [UseProjection]
    public static IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Brand Brand { get; set; } = default!;
}

public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

The expected SDL shape is:

```graphql
type Query {
  products: [Product!]!
}

type Product {
  id: Int!
  name: String!
  brand: Brand!
}

type Brand {
  id: Int!
  name: String!
}
```

Keep the query unmaterialized. Do not call `ToList()`, `ToArray()`, `AsEnumerable()`, or any operation that executes the query before Hot Chocolate runs the middleware.

```csharp
// Correct: the provider can still translate the query.
[UseProjection]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}

// Incorrect: the database query has already executed.
[UseProjection]
public static IEnumerable<Product> GetProducts(CatalogContext db)
{
    return db.Products.ToList();
}
```

# Combining Projection with Paging, Filtering, and Sorting

The order of data middleware attributes is important. Place them in this top-to-bottom order:

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

Use the same relative order if you only need a subset:

```csharp
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}
```

Incorrect ordering can change the field shape seen by the next middleware. The analyzer may report: `Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]`.

Filtering and sorting can also be applied to nested collection properties:

```csharp
public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [UseFiltering]
    [UseSorting]
    public ICollection<Product> Products { get; set; } = [];
}
```

Projection does not apply pagination over relationships in the same way. Use a separate projected field if a nested collection requires its own paging pipeline.

# Projecting a Single Item from an `IQueryable`

Use `[UseFirstOrDefault]` or `[UseSingleOrDefault]` when the resolver should expose a single item but still return `IQueryable<T>` to the middleware pipeline.

```csharp
using HotChocolate.Data;

[QueryType]
public static partial class ProductQueries
{
    [UseFirstOrDefault]
    [UseProjection]
    public static IQueryable<Product> GetProductById(int id, CatalogContext db)
    {
        return db.Products.Where(p => p.Id == id);
    }
}
```

Hot Chocolate exposes the field as a nullable `Product`:

```graphql
type Query {
  productById(id: Int!): Product
}
```

Use `[UseSingleOrDefault]` if more than one matching row should be treated as an error by the underlying query operation.

# Including or Excluding Fields from Projection

Apply `[IsProjected]` as a companion attribute when another resolver needs a member even if the client did not select it.

```csharp
using HotChocolate.Data;

public sealed class Product
{
    [IsProjected]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

`[IsProjected]` is equivalent to `[IsProjected(true)]` and always includes the member in the provider projection.

Use `[IsProjected(false)]` if the field should remain in the GraphQL schema but not be selected by the projection middleware:

```csharp
public sealed class Product
{
    public string Name { get; set; } = string.Empty;
    [IsProjected(false)]
    public string InternalNotes { get; set; } = string.Empty;
}
```

A field excluded from projection still requires a value when a client requests it. Provide a resolver or ensure the object already contains the value.

# Selecting a Projection Scope

The `Scope` property selects a named projection convention. Use this when your schema includes more than one projection provider or convention, such as both queryable and MongoDB projections.

```csharp
builder.Services
    .AddGraphQL()
    .AddProjections("Sql")
    .AddMongoDbProjections("Mongo")
    .AddTypes();

[QueryType]
public static partial class PersonQueries
{
    [UseProjection(Scope = "Mongo")]
    public static IExecutable<Person> GetPersons(IMongoCollection<Person> people)
    {
        return people.AsExecutable();
    }
}
```

When combining provider-specific projection, filtering, and sorting on the same field, use matching scopes for those attributes.

# When to Use Another Configuration Style

Use the attribute when the field can use the inferred projection type and the active convention. `[UseProjection]` exposes `Scope` and the inherited descriptor `Order`, but does not provide a projection type option.

Switch to descriptor-based configuration if you need fluent-only projection options, such as specifying an explicit projection type or configuring fields further:

```csharp
public sealed class ProductQueryResolvers
{
    public IQueryable<Product> GetProducts(CatalogContext db)
    {
        return db.Products;
    }
}

public sealed class ProductQueriesType : ObjectType<ProductQueryResolvers>
{
    protected override void Configure(IObjectTypeDescriptor<ProductQueryResolvers> descriptor)
    {
        descriptor
            .Field(t => t.GetProducts(default!))
            .UseProjection();
    }
}
```

Configure conventions during schema setup if you need provider-specific behavior, a named convention, or custom projection rules.

Do not combine query-context based projection with `[UseProjection]` on the same field. For service-layer patterns using `QueryContext<T>`, let the query context carry projection, filtering, and sorting data.

# Requirements and Limitations

| Requirement or limitation    | What to do                                                                                                                                                                                                       |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Default provider target      | Return `IQueryable<T>` to enable queryable provider translation.                                                                                                                                                 |
| Other data sources           | Register the provider-specific projection package and convention, then select it with `Scope` as needed.                                                                                                         |
| Public setters               | Projected members should have public setters. Without a public setter, default projection materialization can return default values. Provider-specific construction and computed members may behave differently. |
| Custom resolvers             | Fields with custom resolvers inside the selected object are not translated into the parent database projection. Use DataLoader, batching, or a resolver-specific query for those fields.                         |
| Nested projection middleware | A nested field with its own projection middleware is handled as a separate projected field.                                                                                                                      |
| Early materialization        | Avoid executing the query before middleware if you expect database pushdown.                                                                                                                                     |

Attribute members:

| Member  | Type      | Purpose                                                                                                                                                           |
| ------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Scope` | `string?` | Selects a named projection convention. Match the name used with `.AddProjections(name)` or provider-specific registration such as `.AddMongoDbProjections(name)`. |
| `Order` | `int`     | Inherited descriptor attribute order. Prefer physical attribute order unless an advanced scenario requires explicit numeric ordering.                             |

# Troubleshooting

## Analyzer Reports Incorrect Data Attribute Order

Arrange the attributes in this order:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

If you use explicit `Order` values, ensure the numeric order produces the same pipeline.

## HC0099 Reports `[UseProjection]` with Query Context

Remove `[UseProjection]` from any field that uses query-context based projection. Use either projection middleware or query context projection on a field, not both.

## A Projected Field Returns a Default Value

Check that the projected property has a public setter. Also verify whether the field is computed, not mapped by the provider, resolved separately, or excluded with `[IsProjected(false)]`.

## The Database Query Selects More Columns Than Expected

Ensure the resolver returns an unmaterialized `IQueryable<T>`. Provider-required keys, joins, `[IsProjected]` members, and model configuration can add columns. The exact SQL varies by provider.

## A Custom Resolver Field Is Not Optimized

Projection middleware does not translate custom resolver logic inside the selected object. Use DataLoader, batching, or a resolver-specific data access pattern for those fields.

## A Provider Uses the Wrong Projection Convention

Register the provider convention under a scope and set `[UseProjection(Scope = "...")]` on the field. Align filtering and sorting scopes on that field if those provider conventions are also used.

# Next Steps

- Learn about the projection pipeline in [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).
- Configure EF Core in [Entity Framework Integration](/docs/hotchocolate/v16/_leagcy/integrations/entity-framework).
- Set up MongoDB projections in [MongoDB Integration](/docs/hotchocolate/v16/_leagcy/integrations/mongodb).
- Add related data middleware with [UsePaging](/docs/hotchocolate/v16/build/attributes/usepaging), [UseFiltering](/docs/hotchocolate/v16/build/attributes/usefiltering), and [UseSorting](/docs/hotchocolate/v16/build/attributes/usesorting).
