---
title: "Queries"
---

The `Query` root type is the entry point for read operations in a Hot Chocolate schema. It is the only required operation root type, and it should describe the stable ways clients can start reading data.

This page shows how Hot Chocolate v16 maps C# resolver methods to fields on `type Query`, how to split those fields across focused classes, and how to choose root fields that stay useful as your schema grows.

# Separate operations, root types, and C# classes

A client sends a GraphQL `query` operation. That operation selects fields from the schema `Query` root type. In implementation-first Hot Chocolate, C# classes marked with `[QueryType]` contribute those fields.

| Layer            | Example                                                         | What it means                                                  |
| ---------------- | --------------------------------------------------------------- | -------------------------------------------------------------- |
| Client operation | `query GetProduct { productById(id: "UHJvZHVjdDox") { name } }` | The request document a client sends.                           |
| Schema root type | `type Query { productById(id: ID!): Product }`                  | The public read surface clients can select from.               |
| C# contribution  | `[QueryType] public static partial class ProductQueries`        | A source-generated type extension that adds fields to `Query`. |

The same operation can select more than one root field:

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

A typical result shape is:

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

Query fields must be side-effect-free. The execution engine can run sibling query fields concurrently and in any order, so do not mutate application state from a query resolver. Put writes in [mutations](/docs/hotchocolate/v16/building-a-schema/mutations).

# Start with the schema you want

For a catalog, you might want three read entry points:

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

Those fields support common screens:

- `productById` loads one product detail page.
- `brandByName` loads a domain lookup by a meaningful key.
- `products` gives clients a bounded collection entry point.

Nested details, such as `Product.brand`, usually belong on the returned object type instead of the root `Query` type. Use root fields for entry points. Use [object types](./object-types) and type extensions for fields that depend on a parent object already selected by the client.

# Define a single lookup field

Use `[QueryType]` on a `partial` class to contribute fields to the GraphQL `Query` root type. Static query classes are the recommended default because resolver dependencies stay explicit in the method parameters.

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

Register the generated types in your GraphQL setup:

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

Hot Chocolate applies these conventions:

| C# part                               | Schema result                                                                        |
| ------------------------------------- | ------------------------------------------------------------------------------------ |
| `[QueryType]`                         | Adds fields to the `Query` root type.                                                |
| `partial` class                       | Lets the source generator add schema wiring at build time.                           |
| `GetProductByIdAsync`                 | Becomes `productById`; `Get` and `Async` are stripped and the result is camel-cased. |
| `[ID<Product>] int id`                | Becomes `id: ID!` with typed ID behavior.                                            |
| `IProductService products`            | Injected from dependency injection when registered. It is not a GraphQL argument.    |
| `CancellationToken cancellationToken` | Supplied by Hot Chocolate and canceled when the request is aborted.                  |

Call the field with variables:

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

For argument nullability, defaults, renames, and typed IDs, see [Arguments](./arguments) and [Built-in Scalars](./scalars/built-in-scalars).

# Split query fields by domain

GraphQL has one `Query` root type per schema, but your C# code does not need one large class. Hot Chocolate merges multiple `[QueryType]` classes into the same schema root type.

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

Group query classes by domain, feature, bounded context, or ownership. Prefer root fields that represent stable entry points clients can understand. If a field depends on a selected parent value, model it on the object type instead of adding another root field.

# Choose root field shapes intentionally

Use a small set of root field shapes and keep each field focused.

| Shape                | Example field              | Use when                                         | Avoid when                                      | Details                                                                                            |
| -------------------- | -------------------------- | ------------------------------------------------ | ----------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Detail by ID         | `productById(id:)`         | A screen needs one entity by its identifier.     | The field is entity refetch through global IDs. | [Arguments](./arguments), [Relay](/docs/hotchocolate/v16/building-a-schema/relay)                  |
| Business lookup      | `brandByName(name:)`       | The key is meaningful in the domain.             | The lookup can return many matches.             | [Arguments](./arguments)                                                                           |
| Collection           | `products`                 | Clients need a root list entry point.            | The dataset can grow without bounds.            | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)                                 |
| Paginated collection | `products(first:, after:)` | The dataset can be large.                        | The resolver loads every row before paging.     | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)                                 |
| Nested detail        | `Product.brand`            | The value depends on an object already selected. | It is a top-level navigation need.              | [Object Types](./object-types), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |

Global object identification, `node(id:)`, and `[NodeResolver]` are Relay patterns. Use them when clients need entity refetch across the whole graph, not as a replacement for every business lookup.

# Keep arguments focused

Resolver parameters become GraphQL arguments unless Hot Chocolate recognizes them as services or framework values. In this field, only `name` is an argument:

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

Use variables for client input. Use input object types or data middleware when a field needs structured search, filter, or sort input. Use `[GraphQLName]` when the generated field or argument name is not the contract you want.

For detailed argument design, see [Arguments](./arguments), [Input Object Types](./input-object-types), and [Lists and Non-Null](./lists-and-non-null).

# Return the right resolver result shape

Choose a return shape that matches the data source and how clients will use the field.

| Return shape                             | Typical root field         | Use for                                                       | Notes                                                                     |
| ---------------------------------------- | -------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `T` or `T?`                              | Small in-memory value      | Already-loaded values or simple computed reads.               | Keep side effects out of the resolver.                                    |
| `Task<T?>`                               | `productById`              | Async database, service, or API calls.                        | Add `CancellationToken`.                                                  |
| `IQueryable<T>`                          | `products`                 | Data middleware that should compose with a database provider. | Do not materialize before paging, filtering, sorting, or projection.      |
| `Connection<T>` or `Task<Connection<T>>` | `products`                 | Custom paging over non-queryable data.                        | See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).   |
| `QueryContext<T>`                        | Advanced collection field  | v16 projection, filtering, and sorting integration.           | See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections). |
| `IExecutable<T>`                         | Provider-backed collection | Integrations such as MongoDB or custom providers.             | See [Executable](/docs/hotchocolate/v16/api-reference/executable).        |

# Add a collection entry point

For a growing dataset, expose a collection field and let Hot Chocolate data middleware add paging and shaping arguments. Keep provider setup on the data pages and keep the resolver focused on the base query.

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

Register the data features your field uses:

```csharp
builder
    .AddGraphQL()
    .AddTypes()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

The generated SDL has a connection-shaped field. Exact generated input fields depend on your `Product` type and data conventions, but the root shape looks like this:

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

A client can ask for a bounded page:

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

Keep middleware in this order when you combine these attributes: `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`. Do not combine `QueryContext<T>` with `[UseProjection]` on the same field. For provider setup and feature-specific options, see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).

# Secure query fields

Protect root fields that return user-specific or restricted data. Use `HotChocolate.Authorization.AuthorizeAttribute`, not the ASP.NET Core attribute.

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

Authorization setup requires the `HotChocolate.AspNetCore.Authorization` package, ASP.NET Core authorization services, GraphQL `.AddAuthorization()`, and ASP.NET Core authentication and authorization middleware. An unauthenticated request to an authorized field can return an `AUTH_NOT_AUTHENTICATED` GraphQL error and a `null` field value. See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) for setup, roles, policies, and error behavior.

# Use code-first when you need descriptor control

Implementation-first `[QueryType]` classes are the main path in these docs. Use code-first descriptor types when a schema module needs central descriptor control.

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

Register the code-first query type:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<ProductQueriesType>();
```

Use `.AddTypeExtension<T>()`, `[ExtendObjectType]`, or `ObjectTypeExtension<T>` when you need to extend an existing root type without source-generated `[QueryType]` classes. For extension patterns, see [Extending Types](/docs/hotchocolate/v16/building-a-schema/extending-types).

# Keep query execution safe

A well-designed query root is predictable under parallel execution.

- Keep query resolvers side-effect-free. Move writes to mutations.
- Avoid shared mutable state on query classes. Static `[QueryType]` classes help keep state out of the root type.
- Register services with lifetimes that match their behavior. Treat injected services as resolver dependencies, not as hidden schema state.
- Use `CancellationToken` on async resolvers so abandoned requests stop doing work.
- Use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for nested lookups that could otherwise produce N+1 database or API calls.
- Let object fields resolve nested data after the root field returns a parent object.

# Troubleshoot query fields

| Symptom                                           | Likely cause                                                                                                                 | Fix or link                                                                                                                             |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Field is missing from the schema.                 | The class lacks `[QueryType]`, is not `partial`, generated `.AddTypes()` is not registered, or the assembly is not included. | Add `[QueryType]`, keep the class `partial`, and verify generated type registration.                                                    |
| Field name is unexpected.                         | Hot Chocolate stripped `Get` or `Async`, then camel-cased the result.                                                        | Rename the method or apply `[GraphQLName]`. See [Arguments](./arguments) for naming patterns.                                           |
| Service parameter appears as an argument.         | The service is not registered in dependency injection, or the parameter type is not recognized as a service.                 | Register the service and see [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection).                    |
| Argument nullability is unexpected.               | C# nullability or a default value does not match the intended schema.                                                        | See [Arguments](./arguments) and [Lists and Non-Null](./lists-and-non-null).                                                            |
| Filtering, sorting, or projection runs in memory. | The resolver materialized data before middleware could compose provider operations.                                          | Return `IQueryable<T>` where provider composition is expected. See the data middleware pages.                                           |
| Middleware output is unexpected.                  | Attribute order is wrong or provider setup is missing.                                                                       | Use `UsePaging` > `UseProjection` > `UseFiltering` > `UseSorting`; verify registration.                                                 |
| HC0099 appears.                                   | `QueryContext<T>` is combined with `[UseProjection]`.                                                                        | Choose one projection path. See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections).                                   |
| Authorization does not run.                       | The ASP.NET Core `[Authorize]` attribute was used, or authorization was not registered.                                      | Use `HotChocolate.Authorization.AuthorizeAttribute` and follow [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization). |
| Race conditions or duplicate work occur.          | A query resolver mutates state or depends on shared mutable state.                                                           | Move writes to mutations and make resolver services safe for concurrent execution.                                                      |

# Next steps

- Model returned data with [Object Types](./object-types).
- Design field input with [Arguments](./arguments) and [Input Object Types](./input-object-types).
- Add bounded collections with [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- Fetch nested data efficiently with [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
- Protect read fields with [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
