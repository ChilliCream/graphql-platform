---
title: UseProjection attribute
---

Use `[UseProjection]` to let Hot Chocolate build a provider projection from the GraphQL selection set. On database-backed fields this can reduce the selected columns and joins when the underlying provider can translate the projection.

The attribute is the code-first entry point for projections in Hot Chocolate v16. The common path is an Entity Framework Core resolver that returns an unmaterialized `IQueryable<T>`.

# What `[UseProjection]` does

When a client selects a subset of fields, the projection middleware asks the configured projection provider to shape the source query for that selection.

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

For example, a client can request only `name` and `brand.name`:

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

If the resolver returns `IQueryable<Product>`, the default queryable projection provider builds a LINQ `Select` expression for the selected subtree. Entity Framework Core or another LINQ provider then decides how that expression is translated.

Representative SQL for the query above can look like this:

```sql
SELECT "p"."Name", "b"."Id", "b"."Name"
FROM "Products" AS "p"
LEFT JOIN "Brands" AS "b" ON "p"."BrandId" = "b"."Id"
```

The exact query depends on the provider, model mapping, selected fields, and query plan.

# Enable projections

Projections are part of the `HotChocolate.Data` package.

<PackageInstallation packageName="HotChocolate.Data" />

Register projections once when you configure the schema:

```csharp
builder.Services
    .AddGraphQL()
    .AddProjections()
    .AddTypes();
```

Import the data namespace in files that use the attribute:

```csharp
using HotChocolate.Data;
```

# Add projection to a query field

Return the queryable source from the resolver and place `[UseProjection]` on the field.

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

Expected SDL shape:

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

Keep the query unmaterialized. Do not call `ToList()`, `ToArray()`, `AsEnumerable()`, or another operation that executes the query before Hot Chocolate runs the middleware.

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

# Combine projection with paging, filtering, and sorting

Data middleware order matters. Place attributes in this top-to-bottom order:

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

Use the same relative order when you only need a subset:

```csharp
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(CatalogContext db)
{
    return db.Products;
}
```

Incorrect order can change the field shape seen by the next middleware. The analyzer can report: `Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]`.

Filtering and sorting can also apply to nested collection properties:

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

Projection does not project pagination over relationships in the same way. Use a separate projected field when a nested collection needs its own paging pipeline.

# Project a single item from an `IQueryable`

Use `[UseFirstOrDefault]` or `[UseSingleOrDefault]` when the resolver should expose one item while still returning `IQueryable<T>` to the middleware pipeline.

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

Use `[UseSingleOrDefault]` when more than one matching row should be treated as an error by the underlying query operation.

# Include or exclude fields from projection

Use `[IsProjected]` as a companion attribute when another resolver needs a member even if the client did not select it.

```csharp
using HotChocolate.Data;

public sealed class Product
{
    [IsProjected]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
```

`[IsProjected]` is the same as `[IsProjected(true)]`. It always includes the member in the provider projection.

Use `[IsProjected(false)]` when the field should stay in the GraphQL schema but should not be selected by projection middleware:

```csharp
public sealed class Product
{
    public string Name { get; set; } = string.Empty;

    [IsProjected(false)]
    public string InternalNotes { get; set; } = string.Empty;
}
```

A field excluded from projection still needs a value when a client requests it. Provide a resolver or make sure the object already contains the value.

# Select a projection scope

`Scope` selects a named projection convention. Use it when your schema has more than one projection provider or convention, for example queryable projections and MongoDB projections in the same schema.

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

When you combine provider-specific projection, filtering, and sorting on the same field, use matching scopes for those attributes.

# Know when to use another configuration style

Use the attribute when the field can use the inferred projection type and the active convention. `[UseProjection]` exposes `Scope` plus the inherited descriptor `Order`; it does not expose a projection type option.

Switch to descriptor-based configuration when you need fluent-only projection options, such as an explicit projection type or more field configuration:

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

Configure conventions during schema setup when you need provider-specific behavior, a named convention, or custom projection rules.

Do not combine query-context based projection with `[UseProjection]` on the same field. For service-layer patterns that use `QueryContext<T>`, let the query context carry projection, filtering, and sorting data.

# Requirements and limitations

| Requirement or limitation    | What to do                                                                                                                                                                                                       |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Default provider target      | Return `IQueryable<T>` when you want queryable provider translation.                                                                                                                                             |
| Other data sources           | Register the provider-specific projection package and convention, then select it with `Scope` when needed.                                                                                                       |
| Public setters               | Projected members should have public setters. Without a public setter, default projection materialization can return default values. Provider-specific construction and computed members can behave differently. |
| Custom resolvers             | Fields with custom resolvers inside the selected object are not translated into the parent database projection. Use DataLoader, batching, or a resolver-specific query for those fields.                         |
| Nested projection middleware | A nested field with its own projection middleware is handled as a separate projected field.                                                                                                                      |
| Early materialization        | Avoid executing the query before middleware if you expect database pushdown.                                                                                                                                     |

Attribute members:

| Member  | Type      | Purpose                                                                                                                                                           |
| ------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Scope` | `string?` | Selects a named projection convention. Match the name used with `.AddProjections(name)` or provider-specific registration such as `.AddMongoDbProjections(name)`. |
| `Order` | `int`     | Inherited descriptor attribute order. Prefer physical attribute order unless an advanced scenario requires explicit numeric ordering.                             |

# Troubleshooting

## The analyzer reports incorrect data attribute order

Move the attributes into this order:

```csharp
[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
```

If you use explicit `Order` values, make sure the numeric order produces the same pipeline.

## HC0099 reports `[UseProjection]` with query context

Remove `[UseProjection]` from the field that uses query-context based projection. Use either projection middleware or query context projection on a field, not both.

## A projected field returns a default value

Check that the projected property has a public setter. Also check whether the field is computed, not mapped by the provider, resolved separately, or excluded with `[IsProjected(false)]`.

## The database query selects more columns than expected

Confirm that the resolver returns an unmaterialized `IQueryable<T>`. Provider-required keys, joins, `[IsProjected]` members, and model configuration can add columns. Exact SQL varies by provider.

## A custom resolver field is not optimized

Projection middleware does not translate custom resolver logic inside the selected object. Use DataLoader, batching, or a resolver-specific data access pattern for those fields.

## A provider uses the wrong projection convention

Register the provider convention under a scope and set `[UseProjection(Scope = "...")]` on the field. Align filtering and sorting scopes on that field when those provider conventions are also used.

# Next steps

- Learn the projection pipeline in [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).
- Configure EF Core in [Entity Framework Integration](/docs/hotchocolate/v16/integrations/entity-framework).
- Configure MongoDB projections in [MongoDB Integration](/docs/hotchocolate/v16/integrations/mongodb).
- Add related data middleware with [UsePaging](/docs/hotchocolate/v16/build2/attributes/usepaging), [UseFiltering](/docs/hotchocolate/v16/build2/attributes/usefiltering), and [UseSorting](/docs/hotchocolate/v16/build2/attributes/usesorting).
