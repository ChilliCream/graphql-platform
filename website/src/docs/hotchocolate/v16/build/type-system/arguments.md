---
title: "Arguments"
---

Arguments are values that clients provide to specific GraphQL fields. As part of your public schema contract, arguments deserve the same attention as fields and types: name them thoughtfully, assign clear types, document their purpose, and evolve them carefully.

```graphql
type Query {
  productById(id: ID!): Product
  products(search: String, first: Int, after: String): ProductsConnection
}
```

The C# resolver method signature is an implementation detail. Hot Chocolate maps certain resolver parameters to GraphQL arguments, while others are treated as services or framework-provided values.

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
  }
}
```

```json
{
  "id": "UHJvZHVjdDox"
}
```

This page explains arguments as type system members. For details on how arguments are passed to resolvers, see [Resolvers](/docs/hotchocolate/v16/build/resolvers). For guidance on designing structured input, see [Input Object Types](./input-object-types).

# Adding a Field Argument

When a field requires input from the client, use a resolver parameter. In the example below, the query field takes a product ID, uses a catalog service, and supports request cancellation.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        ProductService products,
        CancellationToken cancellationToken)
        => await products.FindByIdAsync(id, cancellationToken);
}
```

Hot Chocolate automatically removes the `Get` prefix and `Async` suffix from resolver method names. For example, `GetProductByIdAsync` becomes `productById` in the schema.

```graphql
type Query {
  productById(id: ID!): Product
}
```

Here, only `id` is exposed to clients. `ProductService` and `CancellationToken` are used internally and do not appear in the schema.

Example client query using variables:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
}
```

# Which Parameters Become Arguments

A resolver parameter is mapped to a GraphQL argument when Hot Chocolate recognizes it as client input. Parameters that represent registered services, parent values, resolver context, or framework values are not exposed as arguments.

```csharp
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

public static async Task<IReadOnlyList<Product>> GetRecommendedProductsAsync(
    string? search,
    [ID<Brand>] int? brandId,
    ProductSearchInput? where,
    [Argument("limit")] int maxResults,
    ProductService products,
    IResolverContext context,
    CancellationToken cancellationToken)
    => await products.GetRecommendedAsync(
        search,
        brandId,
        where,
        maxResults,
        cancellationToken);
```

| Resolver parameter                               | Schema result                | Notes                                                                       |
| ------------------------------------------------ | ---------------------------- | --------------------------------------------------------------------------- |
| `int id`                                         | `id: Int!`                   | Non-null value type argument.                                               |
| `int? limit`                                     | `limit: Int`                 | Nullable value type argument.                                               |
| `string? search`                                 | `search: String`             | Nullable reference type argument when nullable reference types are enabled. |
| `[ID<Product>] int productId`                    | `productId: ID!`             | Mapped to `ID` with typed ID behavior.                                      |
| `ProductSearchInput? where`                      | `where: ProductSearchInput`  | Complex input object argument.                                              |
| `Optional<string?> search`                       | `search: String`             | Advanced: distinguishes omitted from explicit `null`.                       |
| `[Argument("filter")] ProductSearchInput? where` | `filter: ProductSearchInput` | Explicit argument binding and name override.                                |
| `ProductService products`                        | no argument                  | Injected service, not exposed.                                              |
| `CancellationToken cancellationToken`            | no argument                  | Provided by the framework.                                                  |
| `[Parent] Product product`                       | no argument                  | Injected parent value for object field resolvers.                           |
| `IResolverContext context`                       | no argument                  | Access to resolver context.                                                 |

If a parameter unexpectedly appears in the schema, Hot Chocolate may not have recognized it as an injected value. Check your service registration and parameter attributes. If a parameter is missing, verify whether it is a service, parent value, resolver context, generated middleware argument, or explicitly bound with a different name.

# Renaming and Describing Arguments

Use terminology familiar to your clients in the schema, while keeping C# parameter names focused on implementation details. Renaming arguments is a breaking change for clients, so choose stable names early in your API design.

```csharp
using HotChocolate;
using HotChocolate.Types.Relay;

[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [GraphQLName("id")]
        [GraphQLDescription("The product ID.")]
        [ID<Product>]
        int productId,
        ProductService products)
        => products.FindById(productId);
}
```

Expected SDL:

```graphql
type Query {
  product(
    """
    The product ID.
    """
    id: ID!
  ): Product
}
```

You can also use XML documentation comments to describe arguments if XML documentation is enabled:

```csharp
/// <summary>
/// Finds one product.
/// </summary>
/// <param name="productId">The product ID.</param>
public static Product? GetProduct(
    [GraphQLName("id")]
    [ID<Product>]
    int productId,
    ProductService products)
    => products.FindById(productId);
```

Naming tips:

| Goal                  | Guidance                                                                                                    |
| --------------------- | ----------------------------------------------------------------------------------------------------------- |
| Match client language | Use names like `id`, `search`, `first`, `after`, `where`, `order`, or other domain terms clients recognize. |
| Avoid storage leakage | Do not expose database column names unless they are part of your API contract.                              |
| Keep aliases separate | Use GraphQL aliases in client queries for shaping responses, not for renaming schema arguments.             |
| Make binding explicit | Use `[Argument("name")]` to explicitly bind a parameter as an argument and optionally override its name.    |
| Add descriptions      | Use `[GraphQLDescription]`, XML `<param>` comments, or the descriptor `.Description(...)` method.           |

For more on description precedence and XML documentation setup, see [Documentation](/docs/hotchocolate/v16/build/type-system/documentation-comments).

# Required, Optional, and Default Arguments

Argument nullability indicates whether clients must provide a value. Default values specify what happens if the client omits the argument.

| C# parameter                   | SDL                | Meaning                                              |
| ------------------------------ | ------------------ | ---------------------------------------------------- |
| `string name`                  | `name: String!`    | Required when nullable reference types are enabled.  |
| `string? search`               | `search: String`   | Optional and nullable.                               |
| `int limit`                    | `limit: Int!`      | Required value type.                                 |
| `int? limit`                   | `limit: Int`       | Optional nullable value type.                        |
| `[DefaultValue(10)] int limit` | `limit: Int! = 10` | Can be omitted; default is `10`.                     |
| `int limit = 10`               | `limit: Int! = 10` | C# default parameter value becomes a schema default. |

```csharp
using System.ComponentModel;

[QueryType]
public static partial class ProductQueries
{
    public static IReadOnlyList<Product> GetProducts(
        string? search,
        [DefaultValue(20)] int limit,
        ProductService products)
        => products.Search(search, limit);
}
```

Expected SDL:

```graphql
type Query {
  products(search: String, limit: Int! = 20): [Product!]!
}
```

Key input rules:

- A non-null argument without a default must be provided and cannot be `null`.
- A nullable argument can be omitted or set to `null`.
- A default value is used when the client omits the argument.
- An explicit `null` is not the same as omitting the argument.

Use `Optional<T>` if your resolver logic needs to distinguish between omitted input and an explicit `null` value.

```csharp
using HotChocolate;

public static IReadOnlyList<Product> GetProducts(
    Optional<string?> search,
    ProductService products)
{
    if (!search.HasValue)
    {
        return products.GetFeatured();
    }

    return products.Search(search.Value);
}
```

Reserve `Optional<T>` for advanced scenarios with scalar arguments. For partial updates, input object properties are often a better fit. See [Input Object Types](./input-object-types).

# Using List Arguments Effectively

List arguments have two layers of nullability: the list itself and the items within the list.

```graphql
type Query {
  productsByIds(ids: [ID!]!): [Product!]!
  productsByBrandIds(brandIds: [ID!]): [Product!]!
}
```

| SDL shape | Client can omit list | Client can send `null` item | Use when                                                       |
| --------- | -------------------- | --------------------------- | -------------------------------------------------------------- |
| `[ID!]!`  | No                   | No                          | The field requires a list, and null items are not meaningful.  |
| `[ID!]`   | Yes                  | No                          | The list is optional, but all items must be non-null.          |
| `[ID]`    | Yes                  | Yes                         | Null items in the list have a specific meaning in your domain. |

Implementation-first example:

```csharp
using HotChocolate.Types.Relay;

[QueryType]
public static partial class ProductQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        [ID<Product>] IReadOnlyList<int> ids,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdsAsync(ids, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  productsByIds(ids: [ID!]!): [Product!]!
}
```

Design tips:

- Use plural names for list arguments, such as `ids` or `brandIds`.
- Prefer non-null items when null entries do not have a domain meaning.
- Set server-side limits for large list arguments.
- Use pagination for collections that can grow large.
- See [Lists and Non-Null](./lists-and-non-null) and [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) when list size can impact performance or cost.

# Using ID Arguments (and Relay)

Use the `ID` type when clients should treat a value as an opaque identifier. The `[ID]` attribute marks a parameter as a GraphQL `ID`. You can associate the argument with a specific type using `[ID("Product")]` or `[ID<Product>]`.

```csharp
using HotChocolate.Types.Relay;

[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [ID<Product>] int id,
        ProductService products)
        => products.FindById(id);
}
```

Code-first descriptor equivalent:

```csharp
using HotChocolate.Types.Relay;

builder
    .AddGraphQL()
    .AddQueryType(descriptor =>
    {
        descriptor
            .Field("product")
            .Argument("id", a => a.ID<Product>())
            .Resolve(context =>
            {
                var id = context.ArgumentValue<int>("id");
                var products = context.Service<ProductService>();
                return products.FindById(id);
            });
    });
```

`ID` is a scalar type. If you enable global object identification, Hot Chocolate can decode opaque global IDs before your resolver receives the CLR value. An `ID` argument alone does not make a field a Relay node lookup. For node fields, global IDs, and `[Node]` APIs, see [Relay and global object identification](/docs/hotchocolate/v16/build/type-system/relay).

# Choosing Scalar Arguments or Input Objects

Select the argument shape that best fits the client’s needs and allows for safe evolution of your API.

| Use this shape                   | When                                                           | Example                                                                                      |
| -------------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Single scalar argument           | One stable selector or option.                                 | `product(id: ID!)`                                                                           |
| Multiple scalar arguments        | A small set of stable, independent values.                     | `renameBrand(id: ID!, name: String!)`                                                        |
| List argument                    | The client provides a bounded set of values.                   | `productsByIds(ids: [ID!]!)`                                                                 |
| Input object argument            | Values belong together or will evolve together.                | `products(where: ProductSearchInput)`                                                        |
| Single mutation `input` argument | Mutation input is likely to grow or uses mutation conventions. | `addToBasket(input: AddToBasketInput!)`                                                      |
| Generated middleware arguments   | Standard collection behavior.                                  | `products(first: Int, after: String, where: ProductFilterInput, order: [ProductSortInput!])` |

Example using an input object:

```csharp
using HotChocolate.Types.Relay;

public sealed record ProductSearchInput(
    string? Search,
    [property: ID<Brand>] int? BrandId,
    decimal? MaxPrice);

[QueryType]
public static partial class ProductQueries
{
    public static IReadOnlyList<Product> GetProducts(
        ProductSearchInput? where,
        ProductService products)
        => products.Search(where);
}
```

Expected SDL:

```graphql
input ProductSearchInput {
  search: String
  brandId: ID
  maxPrice: Decimal
}

type Query {
  products(where: ProductSearchInput): [Product!]!
}
```

Input object fields cannot themselves have arguments. For details on defaults, `Optional<T>`, immutable constructors, and `@oneOf`, see [Input Object Types](./input-object-types).

# Generated Paging, Filtering, and Sorting Arguments

Some arguments are not defined in your resolver parameters but are added by Hot Chocolate middleware.

```csharp
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
```

| Middleware | Generated argument names           | Configure in                                                                         |
| ---------- | ---------------------------------- | ------------------------------------------------------------------------------------ |
| Paging     | `first`, `after`, `last`, `before` | [Pagination](/docs/hotchocolate/v16/build/pagination)                                |
| Filtering  | `where`                            | [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types) |
| Sorting    | `order`                            | [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types)     |

Notes:

- Apply middleware in this order: `UsePaging`, `UseProjection`, `UseFiltering`, `UseSorting`.
- Filter conventions can rename `where` (for example, `descriptor.ArgumentName("filter")`).
- Sort conventions can rename `order` (for example, `descriptor.ArgumentName("sortBy")`).
- Custom search arguments and generated filter arguments serve different purposes. Generated filters expose operator input types.

# Using Arguments in Mutations

Scalar arguments work well for small, stable mutation actions.

```csharp
using HotChocolate.Types.Relay;

[MutationType]
public static partial class BrandMutations
{
    public static Task<RenameBrandPayload> RenameBrandAsync(
        [ID<Brand>] int id,
        string name,
        BrandService brands,
        CancellationToken cancellationToken)
        => brands.RenameAsync(id, name, cancellationToken);
}

[MutationType]
public static partial class BasketMutations
{
    public static Task<AddToBasketPayload> AddToBasketAsync(
        [ID<Product>] int productId,
        int quantity,
        BasketService baskets,
        CancellationToken cancellationToken)
        => baskets.AddAsync(productId, quantity, cancellationToken);
}
```

```graphql
type Mutation {
  renameBrand(id: ID!, name: String!): RenameBrandPayload!
  addToBasket(productId: ID!, quantity: Int!): AddToBasketPayload!
}
```

As mutations grow, group related values into a single input object.

```csharp
using HotChocolate.Types.Relay;

public sealed record AddToBasketInput(
    [property: ID<Product>] int ProductId,
    int Quantity);

[MutationType]
public static partial class BasketMutations
{
    public static Task<AddToBasketPayload> AddToBasketAsync(
        AddToBasketInput input,
        BasketService baskets,
        CancellationToken cancellationToken)
        => baskets.AddAsync(input.ProductId, input.Quantity, cancellationToken);
}
```

```graphql
type Mutation {
  addToBasket(input: AddToBasketInput!): AddToBasketPayload!
}

input AddToBasketInput {
  productId: ID!
  quantity: Int!
}
```

GraphQL nullability can require fields like `quantity`. Business rules (such as `quantity > 0`, product existence, inventory checks, and authorization) should be handled in your resolver, service, or domain logic. For more on mutation conventions and payload design, see [Mutations](/docs/hotchocolate/v16/build/type-system/operations-mutations).

# Deprecating Arguments Safely

Deprecate an argument when you want clients to migrate to a new argument, but still need to support the old one for a transition period. The deprecated argument must be nullable or have a default value.

```csharp
using HotChocolate;

[QueryType]
public static partial class ProductQueries
{
    public static IReadOnlyList<Product> GetProducts(
        [GraphQLDeprecated("Use search instead.")] string? name,
        string? search,
        ProductService products)
        => products.Search(search ?? name);
}
```

Expected SDL:

```graphql
type Query {
  products(
    name: String @deprecated(reason: "Use search instead.")
    search: String
  ): [Product!]!
}
```

Code-first descriptor equivalent:

```csharp
using HotChocolate.Types;

builder
    .AddGraphQL()
    .AddQueryType(descriptor =>
    {
        descriptor
            .Field("products")
            .Argument("name", a => a.Type<StringType>().Deprecated("Use search instead."))
            .Resolve(context =>
            {
                var name = context.ArgumentValue<string?>("name");
                var products = context.Service<ProductService>();
                return products.Search(name);
            });
    });
```

Never deprecate a required argument unless you first make it nullable or provide a default value. For more on compatibility and versioning, see [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning).

# Configuring Arguments with Descriptors

Use descriptors to configure your schema in code-first style, or when attributes are not suitable for your project.

```csharp
using HotChocolate.Types.Relay;

builder
    .AddGraphQL()
    .AddQueryType(descriptor =>
    {
        descriptor
            .Field("product")
            .Argument("id", a => a
                .ID<Product>()
                .Description("The product ID."))
            .Resolve(context =>
            {
                var id = context.ArgumentValue<int>("id");
                var products = context.Service<ProductService>();
                return products.FindById(id);
            });
    });
```

| Implementation-first                    | Code-first descriptor                                  |
| --------------------------------------- | ------------------------------------------------------ |
| Parameter name                          | `.Argument("name", ...)`                               |
| `[GraphQLName("name")]`                 | Name passed to `.Argument(...)`                        |
| `[Argument("name")]`                    | Explicit argument name or binding                      |
| `[GraphQLDescription]` or XML `<param>` | `.Description("...")`                                  |
| `[DefaultValue(...)]` or C# default     | `.DefaultValue(...)`                                   |
| `[GraphQLType<T>]`                      | `.Type<T>()`                                           |
| `[ID]`                                  | `.ID()`                                                |
| `[ID<Product>]`                         | `.ID<Product>()`                                       |
| `[GraphQLDeprecated]` or `[Obsolete]`   | `.Deprecated(...)`                                     |
| Resolver parameter value                | `context.ArgumentValue<T>("name")`                     |
| Omitted versus explicit value           | `context.ArgumentOptional<T>("name")` or `Optional<T>` |
| Raw GraphQL literal                     | `context.ArgumentLiteral<TNode>("name")`               |

For advanced attribute-based extensions, `ArgumentDescriptorAttribute` is available. Most application schemas can rely on the built-in attributes and descriptor APIs above.

# Validating Argument Input

Use GraphQL type shapes for transport-level validation, and keep business validation in your application code.

| Validation layer                  | What it catches                                                  | Example                                  |
| --------------------------------- | ---------------------------------------------------------------- | ---------------------------------------- |
| GraphQL coercion                  | Missing required value, wrong scalar type, null for non-null.    | `id: ID!` omitted.                       |
| Hot Chocolate schema build        | Invalid input constructor, invalid deprecated required argument. | Immutable input constructor mismatch.    |
| Middleware options                | Paging limits, filtering and sorting conventions.                | `MaxPageSize`, restricted filter fields. |
| Resolver or domain logic          | Business rules and authorization-dependent checks.               | `quantity > 0`, product must exist.      |
| Cost analysis or operation limits | Expensive argument combinations.                                 | Large ID list or nested filter.          |

# Troubleshooting Arguments

| Symptom                                              | Likely cause                                                                                    | Next check                                                      |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Argument appears in schema unexpectedly.             | Parameter was not recognized as a service or framework-injected value.                          | Check DI registration, parameter attributes, and resolver docs. |
| Argument does not appear.                            | Parameter is injected, ignored, generated by middleware, or explicitly bound with another name. | Check resolver signature and middleware attributes.             |
| Client gets a required argument error.               | Non-null argument with no default was omitted.                                                  | Make it nullable, add a default, or require clients to pass it. |
| Client sends `null` and gets an error.               | Argument or input field is non-null.                                                            | Check nullable annotations and variable values.                 |
| Default is not applied.                              | Client sent explicit `null` instead of omitting the argument.                                   | Check the request variables.                                    |
| Input object constructor error occurs at startup.    | Constructor parameters do not match input properties.                                           | See [Input Object Types](./input-object-types).                 |
| Paging, filtering, or sorting arguments are missing. | Middleware package, registration, or attribute is missing.                                      | Check setup and middleware order in the data middleware docs.   |
| Filter is too broad or slow.                         | Default filter input exposes more fields or operations than your use case needs.                | Restrict filter fields and operations.                          |
| ID value is not decoded as expected.                 | ID type name or global ID setup does not match.                                                 | Check `[ID]`, `[ID<T>]`, and Relay setup.                       |
| Domain validation fails.                             | GraphQL validated syntax, types, and nullability, not business rules.                           | Return domain errors or payload errors from application code.   |

# Reference

| Feature                | API                                                               | Namespace or note                            |
| ---------------------- | ----------------------------------------------------------------- | -------------------------------------------- |
| Rename argument        | `[GraphQLName("...")]`                                            | `HotChocolate`                               |
| Force argument binding | `[Argument]`, `[Argument("...")]`                                 | `HotChocolate`                               |
| Describe argument      | `[GraphQLDescription("...")]`, XML `<param>`                      | `HotChocolate` and XML docs setup            |
| Type override          | `[GraphQLType<T>]`, `.Type<T>()`                                  | `HotChocolate`, `HotChocolate.Types`         |
| Default value          | `[DefaultValue(...)]`, C# default parameter, `.DefaultValue(...)` | `System.ComponentModel` for the attribute    |
| ID argument            | `[ID]`, `[ID<T>]`, `.ID()`, `.ID<T>()`                            | `HotChocolate.Types.Relay`                   |
| Deprecation            | `[GraphQLDeprecated]`, `[Obsolete]`, `.Deprecated(...)`           | Arguments must be nullable or have a default |
| Read value             | `IResolverContext.ArgumentValue<T>()`                             | `HotChocolate.Resolvers`                     |
| Detect omission        | `IResolverContext.ArgumentOptional<T>()`, `Optional<T>`           | Advanced omission tracking                   |
| Read literal           | `IResolverContext.ArgumentLiteral<TNode>()`                       | Advanced descriptor and framework scenarios  |
| Extend descriptors     | `ArgumentDescriptorAttribute`                                     | Advanced reusable configuration              |

# Next Steps

- Model structured values with [Input Object Types](./input-object-types).
- Review scalar mapping with [Scalars](./scalars/index) and ID behavior with [Relay](/docs/hotchocolate/v16/build/type-system/relay).
- Tune nullability and collections with [Lists and Non-Null](./lists-and-non-null).
- Add descriptions with [Documentation](/docs/hotchocolate/v16/build/type-system/documentation-comments) and plan changes with [Versioning](/docs/hotchocolate/v16/_leagcy/building-a-schema/versioning).
- Use [Pagination](/docs/hotchocolate/v16/build/pagination), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for generated collection arguments.
- Move from schema shape to execution with [Resolvers](/docs/hotchocolate/v16/build/resolvers) and [Custom Attributes](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes).
- Review [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) when argument combinations can produce expensive operations.
