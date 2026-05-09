---
title: "Queries"
---

The `Query` root type serves as the main entry point for read operations in a Hot Chocolate schema. It is the only operation root type that must be present, and it defines the stable entry points clients use to read data.

This page explains how Hot Chocolate v16 maps C# resolver methods to fields on the `Query` type, how to organize those fields across focused classes, and how to select root fields that remain useful as your schema evolves.

# Separate operations, root types, and C# classes

Clients send GraphQL `query` operations, which select fields from the schema's `Query` root type. In implementation-first Hot Chocolate, C# classes marked with `[QueryType]` contribute fields to this root type.

| Layer            | Example                                                         | What it means                                                  |
| ---------------- | --------------------------------------------------------------- | -------------------------------------------------------------- |
| Client operation | `query GetProduct { productById(id: "UHJvZHVjdDox") { name } }` | The request document a client sends.                           |
| Schema root type | `type Query { productById(id: ID!): Product }`                  | The public read surface clients can select from.               |
| C# contribution  | `[QueryType] public static partial class ProductQueries`        | A source-generated type extension that adds fields to `Query`. |

A single operation can select multiple root fields at once:

```graphql
query ProductScreen($id: ID!, $brand: String!) {
  productById(id: $id) {
    name
    brand {
      name
    }
  }
  brandByName(name: $brand) {
    name
  }
}
```

A typical response for this query might look like:

```json
{
  "data": {
    "productById": {
      "name": "Trail Backpack",
      "brand": {
        "name": "Northwind"
      }
    },
    "brandByName": {
      "name": "Northwind"
    }
  }
}
```

Query fields must not cause side effects. The execution engine may run sibling query fields concurrently and in any order, so avoid mutating application state in a query resolver. Place all write operations in [mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations).

# Start with the schema you want

When designing a catalog, you might define three main read entry points:

```graphql
type Query {
  productById(id: ID!): Product
  brandByName(name: String!): Brand
  products(
    first: Int
    after: String
    last: Int
    before: String
    where: ProductFilterInput
    order: [ProductSortInput!]
  ): ProductsConnection
}
```

These fields support typical use cases:

- `productById` loads a single product detail page.
- `brandByName` provides a domain lookup using a meaningful key.
- `products` offers a bounded collection entry point for clients.

Nested details, like `Product.brand`, should usually be placed on the returned object type rather than the root `Query` type. Reserve root fields for entry points. Use [object types](./object-types) and type extensions for fields that depend on a parent object already selected by the client.

# Define a single lookup field

Apply `[QueryType]` to a `partial` class to add fields to the GraphQL `Query` root type. Using static query classes is recommended, as this keeps resolver dependencies explicit in the method parameters.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed record Product(int Id, string Name, int BrandId);

public interface IProductService
{
    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        IProductService products,
        CancellationToken cancellationToken)
        => await products.GetByIdAsync(id, cancellationToken);
}
```

Register the generated types in your GraphQL configuration:

```csharp
builder
    .AddGraphQL()
    .AddTypes();
```

Expected SDL:

```graphql
type Query {
  productById(id: ID!): Product
}
```

Hot Chocolate uses the following conventions:

| C# part                               | Schema result                                                                        |
| ------------------------------------- | ------------------------------------------------------------------------------------ |
| `[QueryType]`                         | Adds fields to the `Query` root type.                                                |
| `partial` class                       | Lets the source generator add schema wiring at build time.                           |
| `GetProductByIdAsync`                 | Becomes `productById`; `Get` and `Async` are stripped and the result is camel-cased. |
| `[ID<Product>] int id`                | Becomes `id: ID!` with typed ID behavior.                                            |
| `IProductService products`            | Injected from dependency injection when registered. It is not a GraphQL argument.    |
| `CancellationToken cancellationToken` | Supplied by Hot Chocolate and canceled when the request is aborted.                  |

You can call this field using variables:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
  }
}
```

Variables:

```json
{
  "id": "UHJvZHVjdDox"
}
```

For details on argument nullability, default values, renaming, and typed IDs, see [Arguments](./arguments) and [Built-in Scalars](./scalars/built-in-scalars).

# Split query fields by domain

Although GraphQL defines a single `Query` root type per schema, your C# code does not need to use one large class. Hot Chocolate merges multiple `[QueryType]` classes into the same schema root type.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed record Product(int Id, string Name, int BrandId);
public sealed record Brand(int Id, string Name);

public interface IProductService
{
    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}

public interface IBrandService
{
    Task<Brand?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken);
}

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        IProductService products,
        CancellationToken cancellationToken)
        => await products.GetByIdAsync(id, cancellationToken);
}

[QueryType]
public static partial class BrandQueries
{
    public static async Task<Brand?> GetBrandByNameAsync(
        string name,
        IBrandService brands,
        CancellationToken cancellationToken)
        => await brands.GetByNameAsync(name, cancellationToken);
}
```

The generated schema still contains one `type Query`:

```graphql
type Query {
  productById(id: ID!): Product
  brandByName(name: String!): Brand
}
```

Group query classes by domain, feature, bounded context, or ownership. Choose root fields that represent stable entry points clients can easily understand. If a field depends on a selected parent value, place it on the relevant object type rather than adding it as another root field.

# Choose root field shapes intentionally

Use a small, focused set of root field shapes for clarity and maintainability.

| Shape                | Example field              | Use when                                         | Avoid when                                      | Details                                                                               |
| -------------------- | -------------------------- | ------------------------------------------------ | ----------------------------------------------- | ------------------------------------------------------------------------------------- |
| Detail by ID         | `productById(id:)`         | A screen needs one entity by its identifier.     | The field is entity refetch through global IDs. | [Arguments](./arguments), [Relay](/docs/hotchocolate/v16/build/schema-elements/relay) |
| Business lookup      | `brandByName(name:)`       | The key is meaningful in the domain.             | The lookup can return many matches.             | [Arguments](./arguments)                                                              |
| Collection           | `products`                 | Clients need a root list entry point.            | The dataset can grow without bounds.            | [Pagination](/docs/hotchocolate/v16/build/pagination)                                 |
| Paginated collection | `products(first:, after:)` | The dataset can be large.                        | The resolver loads every row before paging.     | [Pagination](/docs/hotchocolate/v16/build/pagination)                                 |
| Nested detail        | `Product.brand`            | The value depends on an object already selected. | It is a top-level navigation need.              | [Object Types](./object-types), [DataLoader](/docs/hotchocolate/v16/build/dataloader) |

Global object identification, `node(id:)`, and `[NodeResolver]` follow Relay patterns. Use these when clients require entity refetching across the entire graph, but not as a substitute for every business lookup.

# Keep arguments focused

Resolver parameters are mapped to GraphQL arguments unless Hot Chocolate recognizes them as services or framework values. In the following example, only `name` is exposed as an argument:

```csharp
[QueryType]
public static partial class BrandQueries
{
    public static async Task<Brand?> GetBrandByNameAsync(
        string name,
        IBrandService brands,
        CancellationToken cancellationToken)
        => await brands.GetByNameAsync(name, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  brandByName(name: String!): Brand
}
```

Client operation:

```graphql
query FindBrand($name: String!) {
  brandByName(name: $name) {
    id
    name
  }
}
```

Use variables for client input. When a field requires structured search, filtering, or sorting, use input object types or data middleware. Apply `[GraphQLName]` if the generated field or argument name does not match your intended contract.

For more on argument design, see [Arguments](./arguments), [Input Object Types](./input-object-types), and [Lists and Non-Null](./lists-and-non-null).

# Return the right resolver result shape

Select a return shape that fits your data source and how clients will use the field.

| Return shape                             | Typical root field         | Use for                                                       | Notes                                                                                             |
| ---------------------------------------- | -------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `T` or `T?`                              | Small in-memory value      | Already-loaded values or simple computed reads.               | Keep side effects out of the resolver.                                                            |
| `Task<T?>`                               | `productById`              | Async database, service, or API calls.                        | Add `CancellationToken`.                                                                          |
| `IQueryable<T>`                          | `products`                 | Data middleware that should compose with a database provider. | Do not materialize before paging, filtering, sorting, or projection.                              |
| `Connection<T>` or `Task<Connection<T>>` | `products`                 | Custom paging over non-queryable data.                        | See [Pagination](/docs/hotchocolate/v16/build/pagination).                                        |
| `QueryContext<T>`                        | Advanced collection field  | v16 projection, filtering, and sorting integration.           | See [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options). |
| `IExecutable<T>`                         | Provider-backed collection | Integrations such as MongoDB or custom providers.             | See [Executable](/docs/hotchocolate/v16/build/execution-internals/executable).                    |

# Add a collection entry point

For datasets that grow over time, expose a collection field and let Hot Chocolate data middleware add paging and shaping arguments. Keep provider setup on the data pages, and keep the resolver focused on the base query.

```csharp
#nullable enable

using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Types;

public sealed class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
}

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products.OrderBy(product => product.Id);
}
```

Register the data features required by your field:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

The generated SDL includes a connection-shaped field. The exact input fields depend on your `Product` type and data conventions, but the root structure resembles the following:

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
```

A client can request a bounded page of results:

```graphql
query ProductsPage {
  products(
    first: 10
    where: { name: { contains: "backpack" } }
    order: [{ name: ASC }]
  ) {
    nodes {
      id
      name
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

When combining these attributes, keep middleware in this order: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`. Do not use `QueryContext<T>` together with `[UseProjection]` on the same field. For provider setup and feature-specific options, see [Pagination](/docs/hotchocolate/v16/build/pagination), [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types).

# Secure query fields

Protect root fields that return user-specific or restricted data. Use `HotChocolate.Authorization.AuthorizeAttribute` instead of the ASP.NET Core attribute.

```csharp
#nullable enable

using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed record User(string Id, string DisplayName);

public interface IUserService
{
    Task<User?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);
}

[QueryType]
public static partial class ViewerQueries
{
    [Authorize]
    public static async Task<User?> GetMeAsync(
        ClaimsPrincipal claimsPrincipal,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        return userId is null
            ? null
            : await users.GetByIdAsync(userId, cancellationToken);
    }
}
```

Expected SDL:

```graphql
type Query {
  me: User @authorize
}
```

To set up authorization, you need the `HotChocolate.AspNetCore.Authorization` package, ASP.NET Core authorization services, GraphQL `.AddAuthorization()`, and ASP.NET Core authentication and authorization middleware. An unauthenticated request to an authorized field may return an `AUTH_NOT_AUTHENTICATED` GraphQL error and a `null` field value. For setup details, roles, policies, and error handling, see [Authorization](/docs/hotchocolate/v16/build/security/authorization).

# Use code-first when you need descriptor control

Implementation-first `[QueryType]` classes are the primary approach in these docs. Use code-first descriptor types when a schema module requires centralized descriptor control.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed record Product(int Id, string Name);

public interface IProductService
{
    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}

public sealed class ProductQueries
{
    public Task<Product?> GetProductByIdAsync(
        int id,
        IProductService products,
        CancellationToken cancellationToken)
        => products.GetByIdAsync(id, cancellationToken);
}

public sealed class ProductQueriesType : ObjectType<ProductQueries>
{
    protected override void Configure(
        IObjectTypeDescriptor<ProductQueries> descriptor)
    {
        descriptor
            .Field(t => t.GetProductByIdAsync(default, default!, default))
            .Name("productById")
            .Argument("id", argument => argument.ID<Product>());
    }
}
```

Register the code-first query type in your GraphQL configuration:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<ProductQueriesType>();
```

Use `.AddTypeExtension<T>()`, `[ExtendObjectType]`, or `ObjectTypeExtension<T>` to extend an existing root type without source-generated `[QueryType]` classes. For extension patterns, see [Extending Types](/docs/hotchocolate/v16/build/schema-elements/extending-types).

# Keep query execution safe

A well-designed query root behaves predictably under parallel execution.

- Keep query resolvers free of side effects. Move all write operations to mutations.
- Avoid shared mutable state in query classes. Static `[QueryType]` classes help keep state out of the root type.
- Register services with lifetimes that match their intended use. Treat injected services as resolver dependencies, not as hidden schema state.
- Use `CancellationToken` in async resolvers so abandoned requests can be canceled.
- Use [DataLoader](/docs/hotchocolate/v16/build/dataloader) for nested lookups that might otherwise cause N+1 database or API calls.
- Let object fields resolve nested data after the root field returns a parent object.

# Troubleshoot query fields

| Symptom                                           | Likely cause                                                                                                                 | Solution or link                                                                                                                     |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Field is missing from the schema.                 | The class lacks `[QueryType]`, is not `partial`, generated `.AddTypes()` is not registered, or the assembly is not included. | Add `[QueryType]`, keep the class `partial`, and verify generated type registration.                                                 |
| Field name is unexpected.                         | Hot Chocolate stripped `Get` or `Async`, then camel-cased the result.                                                        | Rename the method or apply `[GraphQLName]`. See [Arguments](./arguments) for naming patterns.                                        |
| Service parameter appears as an argument.         | The service is not registered in dependency injection, or the parameter type is not recognized as a service.                 | Register the service and see [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection).                       |
| Argument nullability is unexpected.               | C# nullability or a default value does not match the intended schema.                                                        | See [Arguments](./arguments) and [Lists and Non-Null](./lists-and-non-null).                                                         |
| Filtering, sorting, or projection runs in memory. | The resolver materialized data before middleware could compose provider operations.                                          | Return `IQueryable<T>` where provider composition is expected. See the data middleware pages.                                        |
| Middleware output is unexpected.                  | Attribute order is wrong or provider setup is missing.                                                                       | Use `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`; verify registration.                                              |
| HC0099 appears.                                   | `QueryContext<T>` is combined with `[UseProjection]`.                                                                        | Choose one projection path. See [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options).        |
| Authorization does not run.                       | The ASP.NET Core `[Authorize]` attribute was used, or authorization was not registered.                                      | Use `HotChocolate.Authorization.AuthorizeAttribute` and follow [Authorization](/docs/hotchocolate/v16/build/security/authorization). |
| Race conditions or duplicate work occur.          | A query resolver mutates state or depends on shared mutable state.                                                           | Move writes to mutations and make resolver services safe for concurrent execution.                                                   |

# Next steps

- Model returned data using [Object Types](./object-types).
- Design field input with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Add bounded collections using [Pagination](/docs/hotchocolate/v16/build/pagination).
- Fetch nested data efficiently with [DataLoader](/docs/hotchocolate/v16/build/dataloader).
- Protect read fields with [Authorization](/docs/hotchocolate/v16/build/security/authorization).
