---
title: "Extending Types"
---

Type extensions help you keep your Hot Chocolate schema modular as your graph expands across features, models, and teams. A type extension adds fields or configuration to an existing schema type during schema build.

Hot Chocolate merges all these contributions into the final schema type. The printed SDL will show a single `type Product`, one `input CreateProductInput`, or one `interface CatalogItem`. It does not print GraphQL SDL `extend type` blocks for code-first or implementation-first type extensions.

```text
C# files in one server

CatalogQueries        ProductBrandExtensions       ProductReviewExtensions
     |                          |                              |
     v                          v                              v
  Query fields     +      Product fields        +        Product fields
                                |
                                v
Final GraphQL schema: one Query type, one Product type
```

Use this page to modularize your schema within a single Hot Chocolate server. If you need to shape your schema from runtime metadata, see [Dynamic Schemas](/docs/hotchocolate/v16/build/schema-elements/dynamic-schemas). If different services, deployed independently, own separate parts of the graph, see [Fusion](/docs/hotchocolate/v16/_leagcy/fusion).

# Choosing the right extension

| Goal                                                               | Use                                           | Example API                                                                     | Notes                                                                    |
| ------------------------------------------------------------------ | --------------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Split root operation fields by feature                             | Source-generated operation type attributes    | `[QueryType]`, `[MutationType]`, `[SubscriptionType]`                           | Preferred implementation-first pattern.                                  |
| Add resolver fields to an object type                              | Object type extension                         | `[ExtendObjectType<Product>]`, `ObjectTypeExtension<Product>`                   | Use for navigation fields, calculated fields, and models you do not own. |
| Hide an object field or method                                     | Ignore configuration                          | `IgnoreProperties`, `IgnoreFields`, `[GraphQLIgnore]`, `descriptor.Ignore(...)` | Use extension-level ignores when you cannot change the model.            |
| Replace an object field                                            | Bind a resolver to the original member        | `[BindMember(nameof(Product.BrandId))]`                                         | Common for replacing a foreign key scalar with an object field.          |
| Extend an input object type                                        | Code-first input extension                    | `InputObjectTypeExtension`                                                      | Target the GraphQL input type name.                                      |
| Extend a GraphQL interface type                                    | Code-first interface extension                | `InterfaceTypeExtension`                                                        | This changes the interface type itself.                                  |
| Add fields to object types that share a CLR base type or interface | Object type extension with a broad CLR target | `[ExtendObjectType(typeof(IAuditable))]`                                        | Applies to matching object types, not to a GraphQL interface definition. |
| Split one graph across services                                    | Fusion                                        | Fusion docs                                                                     | Out of scope for this page.                                              |

For startup module registration, use `.AddTypeExtension(Type)` or `.AddTypeExtension(ITypeDefinitionExtension)`. For object-specific registration, use the `.AddObjectTypeExtension<...>()` overloads.

# Adding a resolver field to an object type

Use an object type extension when a field should appear on the public GraphQL type, but the resolver logic belongs in another module or you do not want to change the CLR model. The catalog example below exposes `Product.brand` without adding a `Brand` property to `Product`.

```csharp
// Types/Product.cs
#nullable enable

using HotChocolate;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed record Product(
    [property: ID<Product>] int Id,
    string Name,
    [property: ID<Brand>] int BrandId);

public sealed record Brand(
    [property: ID<Brand>] int Id,
    string Name);

public interface IBrandService
{
    Task<Brand?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}
```

```csharp
// Types/ProductBrandExtensions.cs
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType<Product>]
public static partial class ProductBrandExtensions
{
    public static Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandService brands,
        CancellationToken cancellationToken)
        => brands.GetByIdAsync(product.BrandId, cancellationToken);
}
```

Register the extension with the same pattern your project uses for other schema types. When the source generator registers annotated types, include the generated `AddTypes()` call.

```csharp
// Program.cs, generated registration
builder
    .AddGraphQL()
    .AddTypes();
```

When you register manually with `.AddTypeExtension<T>()`, use a non-static extension class:

```csharp
// Types/ProductBrandExtension.cs
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType<Product>]
public sealed class ProductBrandExtension
{
    public Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandService brands,
        CancellationToken cancellationToken)
        => brands.GetByIdAsync(product.BrandId, cancellationToken);
}
```

```csharp
// Program.cs, explicit registration
builder
    .AddGraphQL()
    .AddTypeExtension<ProductBrandExtension>();
```

Expected SDL:

```graphql
type Product {
  id: ID!
  name: String!
  brandId: ID!
  brand: Brand
}

type Brand {
  id: ID!
  name: String!
}
```

Hot Chocolate uses the same field naming conventions as object types. `GetBrandAsync` becomes `brand`; `Get` and `Async` are removed, and the result is camel-cased. Use `[GraphQLName]` or descriptor `.Name(...)` when the field needs an explicit schema name.

For detailed parent value, service injection, and resolver parameter rules, see [Resolvers](/docs/hotchocolate/v16/build/resolvers). For list fields that resolve related entities, prefer a DataLoader to avoid N+1 database or service calls.

# Add an object field with a code-first extension

Use `ObjectTypeExtension<T>` when you want descriptor control or when the extension is part of a reusable schema module.

```csharp
// Types/ProductInventoryExtension.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public interface IInventoryService
{
    string GetStockStatus(int productId);
}

public sealed class ProductInventoryExtension : ObjectTypeExtension<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field("stockStatus")
            .Type<NonNullType<StringType>>()
            .Resolve(context =>
            {
                var product = context.Parent<Product>();
                var inventory = context.Service<IInventoryService>();
                return inventory.GetStockStatus(product.Id);
            });
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypeExtension<ProductInventoryExtension>();
```

Expected SDL:

```graphql
type Product {
  id: ID!
  name: String!
  brandId: ID!
  stockStatus: String!
}
```

# Split root operation types by feature

GraphQL has one `Query`, one `Mutation`, and optionally one `Subscription` root type per schema. Your C# code can split those fields across many classes. In implementation-first code, use `partial` operation classes so the source generator can add schema wiring.

```csharp
// Types/CatalogOperations.cs
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed record CreateProductInput(
    string Name,
    [property: ID<Brand>] int BrandId);

public interface ICatalogService
{
    Task<Product?> GetProductByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetProductsAsync(
        CancellationToken cancellationToken);

    Task<Product> CreateProductAsync(
        CreateProductInput input,
        CancellationToken cancellationToken);
}

[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        ICatalogService catalog,
        CancellationToken cancellationToken)
        => catalog.GetProductByIdAsync(id, cancellationToken);
}

[QueryType]
public static partial class ProductListQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        ICatalogService catalog,
        CancellationToken cancellationToken)
        => catalog.GetProductsAsync(cancellationToken);
}

[MutationType]
public static partial class ProductMutations
{
    public static Task<Product> CreateProductAsync(
        CreateProductInput input,
        ICatalogService catalog,
        CancellationToken cancellationToken)
        => catalog.CreateProductAsync(input, cancellationToken);
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypes();
```

The schema still has one root type for each operation kind:

```graphql
type Query {
  productById(id: ID!): Product
  products: [Product!]!
}

type Mutation {
  createProduct(input: CreateProductInput!): Product!
}
```

Group root classes by domain, feature, or client task. Do not register several independent `Query` root types when the goal is adding fields to the same root type.

If you are not using generated `[QueryType]` classes, extend the root type directly. Prefer `OperationTypeNames` constants over string literals.

```csharp
// Types/ProductQueryFallback.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProductQueryFallback
{
    public Task<IReadOnlyList<Product>> GetProductsAsync(
        ICatalogService catalog,
        CancellationToken cancellationToken)
        => catalog.GetProductsAsync(cancellationToken);
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddQueryType()
    .AddTypeExtension<ProductQueryFallback>();
```

Use `.AddQueryType()` only when no `Query` root type has been registered yet. If another setup call already registers the root type, add the extension to that builder chain.

For operation-specific behavior, see [Queries](./operations-queries), [Mutations](./operations-mutations), and [Subscriptions](./operations-subscriptions).

# Hide fields and methods that should not be public

Public CLR members can become GraphQL fields by convention. Hide persistence details, helper methods, and unstable members before they become part of the client contract.

When you own the model, `[GraphQLIgnore]` on the original member is often the clearest option. When the ignore belongs to another module or you cannot change the model, put it on a type extension.

```csharp
// Types/ProductShapeExtension.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType<Product>(
    IgnoreProperties = new[] { nameof(Product.BrandId) })]
public sealed class ProductShapeExtension
{
}
```

Code-first equivalent:

```csharp
// Types/ProductShapeTypeExtension.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public sealed class ProductShapeTypeExtension : ObjectTypeExtension<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Ignore(t => t.BrandId);
    }
}
```

Expected SDL after hiding `BrandId` and adding `brand` with the extension shown earlier:

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}
```

Use `IgnoreFields` for field-backed members and `IgnoreProperties` for properties. Match the CLR member name, not the GraphQL field name.

# Replace a foreign key field with a navigation field

Use `[BindMember]` when you want one resolver field to replace the field that Hot Chocolate would infer from an existing CLR member. This keeps the public schema focused on graph navigation rather than storage details.

Before replacement:

```graphql
type Product {
  id: ID!
  name: String!
  brandId: ID!
}
```

Replacement extension:

```csharp
// Types/ProductBrandReplacement.cs
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType<Product>]
public static partial class ProductBrandReplacement
{
    [BindMember(nameof(Product.BrandId))]
    public static Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandService brands,
        CancellationToken cancellationToken)
        => brands.GetByIdAsync(product.BrandId, cancellationToken);
}
```

After replacement:

```graphql
type Product {
  id: ID!
  name: String!
  brand: Brand
}
```

For collections of products, use a DataLoader for the related brand lookup. See [DataLoader](/docs/hotchocolate/v16/build/dataloader) for batching and caching patterns.

# Extend an input object type

Use `InputObjectTypeExtension` when you need to contribute input object configuration from another module. The extension targets the GraphQL input type name.

This example starts with a code-first input object type and adds an optional field from another module.

```csharp
// Types/CreateProductInputType.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public sealed class CreateProductInputType : InputObjectType
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Name("CreateProductInput");

        descriptor
            .Field("name")
            .Type<NonNullType<StringType>>();

        descriptor
            .Field("brandId")
            .Type<NonNullType<IdType>>();
    }
}
```

```csharp
// Types/CreateProductInputExtension.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public sealed class CreateProductInputExtension : InputObjectTypeExtension
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Name("CreateProductInput");

        descriptor
            .Field("trackingCode")
            .Type<StringType>()
            .Description("Optional client tracking code for catalog imports.");
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddType<CreateProductInputType>()
    .AddTypeExtension<CreateProductInputExtension>();
```

Expected SDL:

```graphql
input CreateProductInput {
  name: String!
  brandId: ID!
  trackingCode: String
}
```

For CLR-backed input objects, make sure your runtime binding can handle any field you add before you depend on the value. If your goal is to build input types from runtime metadata, use [Dynamic Schemas](/docs/hotchocolate/v16/build/schema-elements/dynamic-schemas) instead.

# Extend an interface type

Use `InterfaceTypeExtension` when you need to change a GraphQL interface definition. This is different from adding fields to object types whose CLR runtime type implements an interface.

Start with an interface and an implementing object type:

```csharp
// Types/CatalogItem.cs
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

[InterfaceType("CatalogItem")]
public interface ICatalogItem
{
    [ID]
    int Id { get; }
}

public sealed record SearchProduct(
    [property: ID<Product>] int Id,
    string Name) : ICatalogItem
{
    public string DisplayName => Name;
}
```

```csharp
// Types/CatalogItemInterfaceExtension.cs
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public sealed class CatalogItemInterfaceExtension : InterfaceTypeExtension
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("CatalogItem");

        descriptor
            .Field("displayName")
            .Type<NonNullType<StringType>>();
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddInterfaceType<ICatalogItem>()
    .AddType<SearchProduct>()
    .AddTypeExtension<CatalogItemInterfaceExtension>();
```

Expected SDL:

```graphql
interface CatalogItem {
  id: ID!
  displayName: String!
}

type SearchProduct implements CatalogItem {
  id: ID!
  name: String!
  displayName: String!
}
```

Every object type that implements `CatalogItem` must also provide `displayName`, otherwise schema validation fails. If you want to add a field to every object type that implements a CLR interface, target the CLR interface with an object type extension:

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types;

namespace Catalog.Types;

[ExtendObjectType(typeof(ICatalogItem))]
public sealed class CatalogItemObjectExtensions
{
    public string GetDisplayName([Parent] ICatalogItem item)
        => item.Id.ToString();
}
```

That pattern extends matching object types. It does not extend the GraphQL interface type.

# Target an extension precisely

| Target                      | API                                                                                                                        | Use when                                               | Risk                                                              |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ----------------------------------------------------------------- |
| CLR object type             | `[ExtendObjectType<Product>]`, `ObjectTypeExtension<Product>`                                                              | You know the runtime type.                             | Lowest risk for object extensions.                                |
| GraphQL object type name    | `[ExtendObjectType("ProductsConnection")]`, non-generic `ObjectTypeExtension` with `descriptor.Name("ProductsConnection")` | The type is generated or lives in another package.     | Names can differ from CLR names.                                  |
| Root operation type         | `[ExtendObjectType(OperationTypeNames.Query)]`                                                                             | You are not using `[QueryType]`.                       | Prefer operation attributes for implementation-first root fields. |
| Base class or CLR interface | `[ExtendObjectType(typeof(IAuditable))]`                                                                                   | Many object types need the same field.                 | Can affect more types than intended.                              |
| Very broad CLR target       | `[ExtendObjectType(typeof(object))]`                                                                                       | Framework-level behavior only.                         | Usually too broad for application code.                           |
| Input object type name      | `InputObjectTypeExtension` with `descriptor.Name("CreateProductInput")`                                                    | You need input type configuration from another module. | Target the final GraphQL input name.                              |
| Interface type name         | `InterfaceTypeExtension` with `descriptor.Name("CatalogItem")`                                                             | You need to extend the interface definition.           | Implementing object types must satisfy new fields.                |

Generated GraphQL names can differ from CLR names because of suffix trimming, naming conventions, and explicit `[GraphQLName]` attributes. For generated connection or edge types, inspect the SDL first, then target the printed type name.

```csharp
// Types/ProductsConnectionExtension.cs
#nullable enable

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace Catalog.Types;

[ExtendObjectType("ProductsConnection")]
public sealed class ProductsConnectionExtension
{
    public int GetTotalVisibleCount([Parent] Connection<Product> connection)
        => connection.Edges.Count;
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypeExtension<ProductsConnectionExtension>();
```

Expected SDL excerpt:

```graphql
type ProductsConnection {
  edges: [ProductsEdge!]
  nodes: [Product!]
  pageInfo: PageInfo!
  totalVisibleCount: Int!
}
```

# Understand merge behavior and field conflicts

Hot Chocolate completes the target type by merging the original type configuration and all registered extensions.

| Extension contribution                                | Merge result                                                                                           |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| New field name                                        | The field is added to the completed type.                                                              |
| Same field name                                       | Field configuration, directives, deprecation, features, and arguments can merge into the target field. |
| Non-repeatable directive with the same directive type | The extension directive replaces the existing directive.                                               |
| Repeatable directive                                  | The extension directive is appended.                                                                   |
| Invalid completed type                                | Schema validation reports an error.                                                                    |

Do not assume that every same-name extension field is a duplicate-field conflict. Merge-by-name is intentional. Duplicate fields, unsatisfied interface fields, or incompatible final definitions can still fail schema validation.

# Use the API quick reference

| Task                                          | Implementation-first API                | Code-first API                                                                       |
| --------------------------------------------- | --------------------------------------- | ------------------------------------------------------------------------------------ |
| Add an object field                           | Method on `[ExtendObjectType<T>]` class | `ObjectTypeExtension<T>` and `descriptor.Field(...)`                                 |
| Access parent object                          | `[Parent] T parent`                     | `context.Parent<T>()`                                                                |
| Inject a service                              | Resolver parameter from DI              | `context.Service<T>()`                                                               |
| Hide a property                               | `IgnoreProperties`, `[GraphQLIgnore]`   | `descriptor.Ignore(t => t.Property)`                                                 |
| Replace a property field                      | `[BindMember(nameof(T.Property))]`      | Use the documented attribute path unless you have validated a descriptor-only setup. |
| Extend by object name                         | `[ExtendObjectType("Foo")]`             | `ObjectTypeExtension` with `descriptor.Name("Foo")`                                  |
| Extend an input object                        | Use code-first                          | `InputObjectTypeExtension` with `descriptor.Name(...)`                               |
| Extend an interface                           | Use code-first                          | `InterfaceTypeExtension` with `descriptor.Name(...)`                                 |
| Include static members on an object extension | `IncludeStaticMembers = true`           | Configure fields explicitly with descriptors.                                        |

# Organize extension classes in a project

Keep extension classes close to the feature that owns the field, but keep the public schema contract reviewable.

```text
Catalog/Types/ProductQueries.cs
Catalog/Types/ProductBrandExtensions.cs
Inventory/Types/ProductInventoryExtensions.cs
Reviews/Types/ProductReviewExtensions.cs
Ordering/Types/ProductMutations.cs
Catalog/Types/CreateProductInputExtension.cs
Catalog/Types/CatalogItemInterfaceExtension.cs
```

Guidelines:

- Group root fields by client task, domain, or ownership.
- Keep resolver methods thin. Put business logic in services and batching in DataLoaders.
- Review the printed SDL when multiple modules contribute to the same type.
- Avoid broad extension targets unless the field truly belongs on every matching object type.

# Troubleshooting

## My extension field does not appear

Check registration first. Use generated `AddTypes()`, `.AddTypeExtension<T>()`, assembly scanning, or your module registration consistently. For source-generated classes, keep the class `partial`. Then verify that the target type name or CLR type matches the completed schema type.

## The field name is not what I expected

Hot Chocolate removes common method prefixes and suffixes such as `Get` and `Async`, then applies GraphQL casing conventions. Use `[GraphQLName]` or descriptor `.Name(...)` for an explicit name. Inspect generated input, connection, edge, and operation names before targeting by string.

## My extension changed too many types

Look for broad targets such as `typeof(object)`, a base class, or a CLR interface used by many object types. Prefer `[ExtendObjectType<Product>]` or a printed GraphQL type name when the field belongs to one type.

## I expected to see `extend type` in the schema

Hot Chocolate type extensions are build-time configuration. The printed schema shows the completed type, not SDL extension syntax.

## I get a schema validation or duplicate field error

Inspect all extensions registered for the completed type. Same-name fields can merge, but the final type must still be valid. If you replace a field, use `[BindMember]` or ignore the original member before adding a field with a different shape. If you extend an interface with a new field, make sure every implementing object type satisfies it.

## My resolver parameter became a GraphQL argument

Hot Chocolate did not bind the parameter as infrastructure. Use `[Parent]` for the parent object and register services in dependency injection. See [Resolvers](/docs/hotchocolate/v16/build/resolvers) for parameter binding details.

## I need multiple services to own the same graph type

This page covers one Hot Chocolate server. Use [Fusion](/docs/hotchocolate/v16/_leagcy/fusion) when multiple deployed services contribute to one composed graph.

# Next steps

- Define base output shapes with [Object Types](./object-types).
- Model operation root fields with [Queries](./operations-queries), [Mutations](./operations-mutations), and [Subscriptions](./operations-subscriptions).
- Define inputs with [Input Object Types](./input-object-types).
- Define shared output contracts with [Interfaces](./interfaces).
- Learn resolver parameters in [Resolvers](/docs/hotchocolate/v16/build/resolvers).
- Avoid N+1 lookups with [DataLoader](/docs/hotchocolate/v16/build/dataloader).
- Extend generated connection and edge types with [Pagination](/docs/hotchocolate/v16/build/pagination).
- Use `[Node]` and global object identification with [Relay](/docs/hotchocolate/v16/build/schema-elements/relay).
- Review attribute names in [Custom Attributes](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes).
