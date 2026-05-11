---
title: UsePaging attribute
---

The `[UsePaging]` attribute adds Hot Chocolate's cursor paging middleware to a field that returns a collection. This attribute is the local, code-first entry point for connection paging in Hot Chocolate.

When you page a field, it returns a connection rather than a raw list. The connection provides clients with page arguments, opaque cursors, item nodes, and page metadata.

`[UsePaging]` implements cursor-based paging. For offset paging, use the separate `[UseOffsetPaging]` attribute, which returns collection segment semantics. Do not apply both paging attributes to the same field.

# Adding cursor paging to a query field

Begin with a resolver that returns a stable, ordered collection. Apply `[UsePaging]` to the resolver method or property:

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public sealed class ProductStore
{
    private static readonly Product[] s_products =
    [
        new() { Id = 1, Name = "Banana" },
        new() { Id = 2, Name = "Coffee" }
    ];

    public IQueryable<Product> Products => s_products.AsQueryable();
}
```

This produces the following SDL shape:

```graphql
type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
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

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

Example client query:

```graphql
query GetProducts($first: Int!, $after: String) {
  products(first: $first, after: $after) {
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

To fetch the next page, use the `endCursor` from one response as the `after` variable for the next. Cursors are opaque tokens representing positions. Clients should store and reuse them, not attempt to parse their contents.

# Where to apply paging

You can apply `[UsePaging]` to methods and properties that become object or interface fields.

## Paging a query method

```csharp
using HotChocolate;
using HotChocolate.Types;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static IQueryable<Product> GetProducts(ProductStore store)
    {
        return store.Products.OrderBy(p => p.Id);
    }
}
```

## Paging a property or object field

```csharp
using HotChocolate.Types;

public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    [UsePaging]
    public IQueryable<Product> Products { get; init; } = Enumerable.Empty<Product>().AsQueryable();
}
```

## Paging an interface field

```csharp
using HotChocolate.Types;

public interface IHasProducts
{
    [UsePaging]
    IQueryable<Product> Products { get; }
}
```

## Paging an existing field from a type extension

When extending a CLR type, use `[BindMember]` if the extension configures a field that already exists:

```csharp
using HotChocolate;
using HotChocolate.Types;

[ExtendObjectType(typeof(ProductCatalog))]
public sealed class ProductCatalogExtensions
{
    [UsePaging]
    [BindMember(nameof(ProductCatalog.Products))]
    public IQueryable<Product> GetProducts([Parent] ProductCatalog catalog)
    {
        return catalog.Products.OrderBy(p => p.Id);
    }
}

public sealed class ProductCatalog
{
    public IQueryable<Product> Products { get; init; } = Enumerable.Empty<Product>().AsQueryable();
}
```

Supported resolver return types include:

| Result shape                             | Use it when                                                                       |
| ---------------------------------------- | --------------------------------------------------------------------------------- |
| `IQueryable<T>`                          | A database provider can translate paging into source queries.                     |
| `IEnumerable<T>`                         | The collection is already in memory.                                              |
| `Connection<T>` or `Task<Connection<T>>` | Your service layer, external API, or custom cursor logic already produced a page. |

Scalar values and single objects are not valid paging sources. If Hot Chocolate cannot infer the node type, specify it with the attribute constructor.

Cursor paging requires deterministic ordering. Always add a stable unique key, such as `Id`, as the final ordering term when building cursors in your data source or service layer.

# Configuring page size and API limits

Set per-field limits when a field needs different behavior from the global paging defaults:

```csharp
using HotChocolate.Types;

[UsePaging(DefaultPageSize = 20, MaxPageSize = 100, RequirePagingBoundaries = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

| Option                    | Default | Effect                                                             |
| ------------------------- | ------- | ------------------------------------------------------------------ |
| `DefaultPageSize`         | `10`    | Number of items returned when the client omits `first` and `last`. |
| `MaxPageSize`             | `50`    | Largest accepted value for `first` or `last`.                      |
| `RequirePagingBoundaries` | `false` | Requires the client to send `first` or `last`.                     |

For public APIs, keep `MaxPageSize` conservative. This setting also affects Hot Chocolate's [cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis), as cost calculation uses it as the assumed list size for the field.

# Controlling backward paging

Backward pagination uses `last` and `before` and is enabled by default. Disable it if reverse paging is not supported by your data source or is too expensive for the field:

```csharp
using HotChocolate.Types;

[UsePaging(AllowBackwardPagination = false)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

With backward paging disabled, clients can only page forward using `first` and `after`.

# Adding totalCount for item counts

By default, `totalCount` is not included in the generated connection. Enable it per field when clients need the total number of matching items:

```csharp
using HotChocolate.Types;

[UsePaging(IncludeTotalCount = true)]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

Example client query:

```graphql
query GetProductCount {
  products(first: 10) {
    totalCount
    nodes {
      id
      name
    }
  }
}
```

For `IQueryable<T>` and `IEnumerable<T>` sources, Hot Chocolate computes the count through the paging provider. For `Connection<T>` results, you must set the count yourself:

```csharp
using HotChocolate.Types.Pagination;

return new Connection<Product>(
    edges,
    pageInfo,
    totalCount: totalCount);
```

Counting can add cost to your data source. Enable it only when the client experience requires it.

# Renaming the generated connection type

By default, Hot Chocolate infers connection and edge type names from the field name. For example, a field named `products` generates `ProductsConnection` and `ProductsEdge`.

Use `ConnectionName` when the public schema needs a stable or clearer generated type name:

```csharp
using HotChocolate.Types;

[UsePaging(ConnectionName = "TeamMembers")]
public static IQueryable<User> GetUsers(UserStore store)
{
    return store.Users.OrderBy(u => u.Id);
}
```

Expected generated type names:

```graphql
type Query {
  users(
    first: Int
    after: String
    last: Int
    before: String
  ): TeamMembersConnection
}

type TeamMembersConnection {
  pageInfo: PageInfo!
  edges: [TeamMembersEdge!]
  nodes: [User!]
}
```

Set `InferConnectionNameFromField = false` if you want type-based naming instead of field-based naming:

```csharp
[UsePaging(InferConnectionNameFromField = false)]
public static IQueryable<User> GetUsers(UserStore store)
{
    return store.Users.OrderBy(u => u.Id);
}
```

# Specifying the node GraphQL type

Pass a schema type to the attribute constructor when the node type cannot be inferred or when the field should expose a specific object type:

```csharp
using HotChocolate.Types;

[UsePaging(typeof(ProductType))]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
    }
}
```

The constructor argument is the GraphQL schema type for a single item. Do not pass the collection type.

# Composing paging with projections, filtering, and sorting

Apply data middleware attributes in this top-to-bottom order:

```csharp
using HotChocolate.Data;
using HotChocolate.Types;

[UsePaging]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(ProductStore store)
{
    return store.Products.OrderBy(p => p.Id);
}
```

Paging wraps the field as a connection. Projections, filtering, and sorting transform the underlying query. If clients can sort dynamically, ensure a unique final sort key in your data-access or service layer so cursors remain stable.

# Choosing a paging provider

The default provider supports `IEnumerable<T>` and `IQueryable<T>`. Source-specific providers support other result shapes, such as MongoDB executables.

Register a provider:

```csharp
builder
    .AddGraphQL()
    .AddMongoDbPagingProviders(providerName: "MongoDB");
```

Select the provider on the field:

```csharp
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using MongoDB.Driver;

[UsePaging(ProviderName = "MongoDB")]
public static IExecutable<Product> GetProducts(IMongoCollection<Product> products)
{
    return products.AsExecutable();
}
```

If `ProviderName` is not set, Hot Chocolate selects a provider based on the source type. If the provider cannot be inferred, it uses the first registered provider. Set `ProviderName` when multiple providers are registered or when you want the schema rule to be explicit.

# Returning Connection<T> for service-layer paging

Return `Connection<T>` when another layer already handles paging, cursor creation, or data-source navigation. The field still requires `[UsePaging]` so Hot Chocolate exposes connection arguments and generates connection types.

To inject resolver arguments for paging:

```csharp
builder
    .AddGraphQL()
    .AddPagingArguments();
```

Use the arguments in your resolver and convert the service-layer page to a connection:

```csharp
using GreenDonut.Data;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    public static async Task<Connection<Product>> GetProductsAsync(
        PagingArguments pagingArguments,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService
            .GetProductsAsync(pagingArguments, cancellationToken)
            .ToConnectionAsync();
    }
}
```

This approach is useful for external APIs, service-layer keyset paging, custom cursor values, or when you need precise control over `PageInfo` and `totalCount`. See [Pagination](/docs/hotchocolate/v16/build/pagination) for manual `Connection<T>` construction.

# Moving shared or advanced configuration out of attributes

Use attributes when configuration is static, local, and short enough to read at the field declaration. Move configuration to global options or descriptors when the rule is shared, advanced, or generated by code.

To set global defaults for many fields:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = 25;
        options.MaxPageSize = 100;
        options.IncludeTotalCount = true;
    });
```

Use a descriptor when configuration belongs outside the resolver class or when you need `PagingOptions` members that the attribute does not expose:

```csharp
using GreenDonut.Data;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("products")
            .UsePaging(options: new PagingOptions
            {
                IncludeNodesField = false,
                NullOrdering = NullOrdering.NativeNullsLast
            })
            .Resolve(context => context.Service<ProductStore>().Products.OrderBy(p => p.Id));
    }
}
```

Descriptor-based configuration is preferred for reusable field setup, descriptor-only schemas, conditional options, third-party CLR types, or advanced paging options.

# Attribute property reference

| Attribute member                        | Default             | Use it for                                                         |
| --------------------------------------- | ------------------- | ------------------------------------------------------------------ |
| `UsePagingAttribute(Type? type = null)` | inferred node type  | Sets the schema type for each node.                                |
| `ConnectionName`                        | inferred from field | Sets the base name for generated connection and edge types.        |
| `DefaultPageSize`                       | `10`                | Sets the item count used when the client omits `first` and `last`. |
| `MaxPageSize`                           | `50`                | Sets the maximum accepted `first` or `last` value.                 |
| `IncludeTotalCount`                     | `false`             | Adds `totalCount` to the connection type.                          |
| `AllowBackwardPagination`               | `true`              | Enables `last` and `before` support.                               |
| `RequirePagingBoundaries`               | `false`             | Requires clients to send `first` or `last`.                        |
| `InferConnectionNameFromField`          | `true`              | Uses the field name as the generated connection name source.       |
| `ProviderName`                          | `null`              | Selects a named paging provider.                                   |

The following `PagingOptions` members are not attribute properties: `NullOrdering`, `IncludeNodesField`, `EnableRelativeCursors`, `RelativeCursorFields`, and `PageInfoFields`. Configure these with `ModifyPagingOptions` or a descriptor `.UsePaging(options: ...)`.

`CollectionSegmentName` and `InferCollectionSegmentNameFromField` are for `[UseOffsetPaging]`, not `[UsePaging]`.

# Troubleshooting paging attributes

## My field is not paginated

Ensure `[UsePaging]` is applied to the resolver method or property that Hot Chocolate exposes as the field. Also, check that the field returns `IQueryable<T>`, `IEnumerable<T>`, `Connection<T>`, or an async wrapper around a valid pageable shape.

## Schema creation fails after adding UsePaging

Do not apply `[UsePaging]` to scalar values or single objects. If the field returns a collection but the node type cannot be inferred, specify the node schema type with `[UsePaging(typeof(ProductType))]`.

## Filtering, sorting, or projections behave incorrectly

Check the attribute order. Use `[UsePaging]`, then `[UseProjection]`, then `[UseFiltering]`, then `[UseSorting]`.

## totalCount is missing

Set `IncludeTotalCount = true`. If your resolver returns `Connection<T>`, provide the total count to the `Connection<T>` constructor.

## Pages repeat or skip items

Make the ordering deterministic. Add a unique key as the final ordering term in the query or service layer.

## I need null ordering, relative cursors, or nodes field control

Use `ModifyPagingOptions` or a descriptor `.UsePaging(options: ...)`. These settings are not exposed by `[UsePaging]`.

## The wrong paging provider is used

Register the provider and set `ProviderName` on the field if the source type does not select the provider you expect.

# Next steps

- [Pagination](/docs/hotchocolate/v16/build/pagination) for connection concepts and manual `Connection<T>` construction.
- [Relay Connections](../type-system/relay/connections) for the generated connection schema contract.
- [Attributes overview](./) for attribute and descriptor tradeoffs.
- [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options), [Filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/filter-types), and [Sorting](/docs/hotchocolate/v16/build/filtering-sorting-projections/sort-types) for the data middleware stack.
- [Field middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware) for execution ordering.
- [Paging options](/docs/hotchocolate/v16/build/server-configuration/schema-options#paging-options-modifypagingoptions) for global configuration.
- [Cost Analysis](/docs/hotchocolate/v16/build/security/cost-analysis) for query cost limits.
- [Integrations](/docs/hotchocolate/v16/_leagcy/integrations) for provider setup.
