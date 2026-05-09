---
title: "Arguments"
---

Arguments are values clients pass to a specific GraphQL field. They are part of the public schema contract, so name them, type them, document them, and evolve them with the same care as fields and types.

```graphql
type Query {
  productById(id: ID!): Product
  products(search: String, first: Int, after: String): ProductsConnection
}
```

The C# resolver signature is an implementation detail. Hot Chocolate maps some resolver parameters to GraphQL arguments and treats other parameters as services or framework values.

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

This page focuses on arguments as schema elements. For resolver execution details, see [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers). For structured input design, see [Input Object Types](./input-object-types).

# Add a field argument

Use a resolver parameter when a field needs client input. The following query field accepts a product ID, uses a catalog service, and honors request cancellation.

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

Hot Chocolate strips the `Get` prefix and `Async` suffix from resolver method names. `GetProductByIdAsync` becomes `productById`.

```graphql
type Query {
  productById(id: ID!): Product
}
```

Only `id` is public. `ProductService` and `CancellationToken` are resolver infrastructure and do not appear in the schema.

Client query with variables:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
}
```

# Know which parameters become arguments

A resolver parameter becomes a GraphQL argument when Hot Chocolate classifies it as client input. Registered services, parent values, resolver context values, and framework values are not arguments.

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
| `[ID<Product>] int productId`                    | `productId: ID!`             | Rewritten to `ID` with typed ID behavior.                                   |
| `ProductSearchInput? where`                      | `where: ProductSearchInput`  | Complex input object argument.                                              |
| `Optional<string?> search`                       | `search: String`             | Advanced pattern for omitted versus explicit `null`.                        |
| `[Argument("filter")] ProductSearchInput? where` | `filter: ProductSearchInput` | Explicit argument binding and name override.                                |
| `ProductService products`                        | no argument                  | Registered service injection.                                               |
| `CancellationToken cancellationToken`            | no argument                  | Framework-provided request cancellation.                                    |
| `[Parent] Product product`                       | no argument                  | Parent value injection on object field resolvers.                           |
| `IResolverContext context`                       | no argument                  | Resolver context access.                                                    |

If a parameter appears in the schema unexpectedly, Hot Chocolate did not classify it as an injected value. Check service registration and parameter attributes. If a parameter does not appear, check whether it is a service, parent value, resolver context, generated middleware argument, or explicitly bound with another name.

# Rename and describe an argument

Use client vocabulary in the schema and keep C# parameter names focused on implementation. Argument renames are breaking changes for clients, so pick stable names early.

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

XML documentation comments can also describe arguments when XML documentation is enabled:

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

Naming guidance:

| Goal                  | Guidance                                                                                                          |
| --------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Match client language | Prefer `id`, `search`, `first`, `after`, `where`, `order`, or domain names clients understand.                    |
| Avoid storage leakage | Do not expose database column names unless they are part of the API contract.                                     |
| Keep aliases separate | Use GraphQL aliases in client operations for one response shape, not schema renames.                              |
| Make binding explicit | Use `[Argument("name")]` when you want to state that a parameter is an argument and optionally override its name. |
| Add descriptions      | Use `[GraphQLDescription]`, XML `<param>` comments, or descriptor `.Description(...)`.                            |

For description precedence and XML documentation setup, see [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation).

# Choose required, optional, or defaulted

Argument nullability tells clients whether they must provide a value. Defaults tell clients what happens when they omit the argument.

| C# parameter                   | SDL                | Meaning                                              |
| ------------------------------ | ------------------ | ---------------------------------------------------- |
| `string name`                  | `name: String!`    | Required when nullable reference types are enabled.  |
| `string? search`               | `search: String`   | Optional and nullable.                               |
| `int limit`                    | `limit: Int!`      | Required value type.                                 |
| `int? limit`                   | `limit: Int`       | Optional nullable value type.                        |
| `[DefaultValue(10)] int limit` | `limit: Int! = 10` | Can be omitted, default is `10`.                     |
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

Important input rules:

- A non-null argument without a default must be provided and cannot be `null`.
- A nullable argument can be omitted or provided as `null`.
- A default value applies when the client omits the argument.
- An explicit `null` is not the same as omission.

Use `Optional<T>` when resolver logic needs to distinguish omitted input from explicit `null`.

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

Keep `Optional<T>` in regular scalar arguments for advanced cases. Input object properties are often the better place for partial update semantics. See [Input Object Types](./input-object-types).

# Use list arguments intentionally

A list argument has two nullability layers: the list value and the list items.

```graphql
type Query {
  productsByIds(ids: [ID!]!): [Product!]!
  productsByBrandIds(brandIds: [ID!]): [Product!]!
}
```

| SDL shape | Client can omit list | Client can send `null` item | Use when                                                               |
| --------- | -------------------- | --------------------------- | ---------------------------------------------------------------------- |
| `[ID!]!`  | No                   | No                          | The field needs at least a list value, and null items have no meaning. |
| `[ID!]`   | Yes                  | No                          | The list is optional, but provided items must be values.               |
| `[ID]`    | Yes                  | Yes                         | Null list items carry a domain meaning.                                |

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

- Use plural argument names for lists, such as `ids` or `brandIds`.
- Prefer non-null items when null entries have no domain meaning.
- Set server-side limits for large list arguments.
- Use pagination for unbounded collections.
- Review [Lists and Non-Null](./lists-and-non-null), and [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) when list size can affect cost.

# Use ID arguments without overclaiming Relay behavior

Use `ID` when clients should treat a value as an opaque identifier. `[ID]` marks a parameter as GraphQL `ID`. `[ID("Product")]` or `[ID<Product>]` associates the argument with a specific type name.

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

`ID` is a scalar contract. When global object identification is configured, Hot Chocolate can decode opaque global IDs before your resolver receives the CLR value. An `ID` argument does not by itself make the field a Relay node lookup. Continue with [Relay and global object identification](/docs/hotchocolate/v16/building-a-schema/relay) for node fields, global IDs, and `[Node]` APIs.

# Choose scalar arguments or an input object

Use the smallest argument shape that matches the client task and can evolve safely.

| Use this shape                   | When                                                           | Example                                                                                      |
| -------------------------------- | -------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Single scalar argument           | One stable selector or option.                                 | `product(id: ID!)`                                                                           |
| Multiple scalar arguments        | A small set of stable, independent values.                     | `renameBrand(id: ID!, name: String!)`                                                        |
| List argument                    | The client provides a bounded set of values.                   | `productsByIds(ids: [ID!]!)`                                                                 |
| Input object argument            | Values belong together or will evolve together.                | `products(where: ProductSearchInput)`                                                        |
| Single mutation `input` argument | Mutation input is likely to grow or uses mutation conventions. | `addToBasket(input: AddToBasketInput!)`                                                      |
| Generated middleware arguments   | Standard collection behavior.                                  | `products(first: Int, after: String, where: ProductFilterInput, order: [ProductSortInput!])` |

Input object example:

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

Input object fields cannot have arguments. Defaults, `Optional<T>`, immutable constructors, and `@oneOf` belong on the input object page. See [Input Object Types](./input-object-types).

# Understand generated paging, filtering, and sorting arguments

Some arguments are added by Hot Chocolate middleware rather than by your resolver parameters.

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

| Middleware | Generated argument names           | Configure in                                                       |
| ---------- | ---------------------------------- | ------------------------------------------------------------------ |
| Paging     | `first`, `after`, `last`, `before` | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) |
| Filtering  | `where`                            | [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering)   |
| Sorting    | `order`                            | [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)       |

Notes:

- Apply middleware in this order: `UsePaging`, `UseProjection`, `UseFiltering`, `UseSorting`.
- Filter conventions can rename `where`, for example `descriptor.ArgumentName("filter")`.
- Sort conventions can rename `order`, for example `descriptor.ArgumentName("sortBy")`.
- Do not design custom search arguments and generated filter arguments as if they were the same feature. Generated filters expose operator input types.

# Use arguments in mutations carefully

Scalar mutation arguments work well for small, stable actions.

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

When a mutation grows, move related values into a single input object.

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

GraphQL nullability can require `quantity`. Business rules such as `quantity > 0`, product existence, inventory checks, and authorization belong in resolver, service, or domain logic. For mutation conventions and payload design, see [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations).

# Deprecate arguments safely

Deprecate an argument when clients should migrate to another argument while the old one still works. The deprecated argument must be nullable or have a default value.

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

Do not deprecate a required argument unless you first make it nullable or provide a default value. For broader compatibility guidance, see [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).

# Configure arguments with descriptors

Use descriptors when you configure a schema in code-first style or when attributes are not the right fit for your project.

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

`ArgumentDescriptorAttribute` is available for advanced attribute-based extensions. Most application schemas can use the built-in attributes and descriptor APIs above.

# Validate argument input at the right layer

Use GraphQL type shapes for transport-level validation and keep business validation in application code.

| Validation layer                  | What it catches                                                  | Example                                  |
| --------------------------------- | ---------------------------------------------------------------- | ---------------------------------------- |
| GraphQL coercion                  | Missing required value, wrong scalar type, null for non-null.    | `id: ID!` omitted.                       |
| Hot Chocolate schema build        | Invalid input constructor, invalid deprecated required argument. | Immutable input constructor mismatch.    |
| Middleware options                | Paging limits, filtering and sorting conventions.                | `MaxPageSize`, restricted filter fields. |
| Resolver or domain logic          | Business rules and authorization-dependent checks.               | `quantity > 0`, product must exist.      |
| Cost analysis or operation limits | Expensive argument combinations.                                 | Large ID list or nested filter.          |

# Troubleshoot arguments

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

# Next steps

- Model structured values with [Input Object Types](./input-object-types).
- Review scalar mapping with [Scalars](./scalars/index) and ID behavior with [Relay](/docs/hotchocolate/v16/building-a-schema/relay).
- Tune nullability and collections with [Lists and Non-Null](./lists-and-non-null).
- Add descriptions with [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation) and plan changes with [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning).
- Use [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting) for generated collection arguments.
- Move from schema shape to execution with [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) and [Custom Attributes](/docs/hotchocolate/v16/api-reference/custom-attributes).
- Review [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) when argument combinations can produce expensive operations.
