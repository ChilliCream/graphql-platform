---
title: Global Identifiers
---

Global identifiers allow clients to recognize an object as the same entity wherever it appears in your graph. For example, a database might have both `Product.Id = 1` and `Brand.Id = 1`, but a normalized client cache requires public IDs that never collide.

Hot Chocolate addresses this by serializing an opaque value that includes both the GraphQL type name and the raw ID. Clients store this value and return it unchanged. With Node support enabled, clients can refetch objects using the standard `node(id:)` and `nodes(ids:)` fields.

```graphql
interface Node {
  id: ID!
}

type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}

type Product implements Node {
  id: ID!
  name: String!
}
```

This page covers global identifiers and node lookup. For cursor-based lists, see [Connections](./connections). For tenant-local keys, SKU keys, or other composite values, refer to [Complex IDs](./complex-ids).

# Understanding the ID Layers

When designing your schema, keep these terms distinct:

| Layer                   | Example                       | Description                                                                                                                  |
| ----------------------- | ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Database primary key    | `1`                           | A local storage value, which may repeat across tables or services.                                                           |
| GraphQL `ID` scalar     | `id: ID!`                     | A string-like GraphQL scalar for identity, but not globally unique by itself.                                                |
| Hot Chocolate global ID | `"UHJvZHVjdDox"`              | An opaque serialized value containing the GraphQL type name and raw ID. The exact value depends on serializer configuration. |
| Relay Node ID           | `Product.id` on a `Node` type | A global ID that can be sent to `node(id:)` or `nodes(ids:)` to refetch the object.                                          |

A typical request flow is as follows:

1. Your resolver returns a `Product` with raw `Id = 1`.
2. Hot Chocolate serializes the public `id` field as an opaque global ID.
3. The client stores the ID with `__typename` in its normalized cache.
4. The client later sends the same value to `node(id: $id)`.
5. Hot Chocolate parses the type name and raw ID.
6. The node resolver receives the raw ID and loads the object.

Clients should never be required to decode or assemble global IDs. Treat the decoded structure as a server-side implementation detail.

# Enabling Global Object Identification

Register global object identification once during schema configuration.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddGlobalObjectIdentification();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public sealed class Query
{
    public Product GetProduct()
        => new() { Id = 1, Name = "Trail Backpack" };
}

[Node]
public sealed class Product
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public static Task<Product?> GetAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult<Product?>(id == 1
            ? new Product { Id = 1, Name = "Trail Backpack" }
            : null);
}
```

The snippet includes a minimal node type so the schema can expose the node fields immediately. `AddGlobalObjectIdentification()` adds the `Node` interface, adds `Query.node`, adds `Query.nodes`, and registers a default `INodeIdSerializer` when you have not registered one. You still need object types with Node configuration and resolvers before node lookup can return useful results.

Use options when you need to change the schema contract or request cost.

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification(options =>
    {
        options.MaxAllowedNodeBatchSize = 25;
    });
```

| Option                        | Default | When to change it                                                                                           |
| ----------------------------- | ------- | ----------------------------------------------------------------------------------------------------------- |
| `RegisterNodeInterface`       | `true`  | Disable only for advanced schema composition scenarios where another component registers the Node contract. |
| `AddNodesField`               | `true`  | Disable the plural lookup field if clients should not batch refetch through `nodes(ids:)`.                  |
| `EnsureAllNodesCanBeResolved` | `true`  | Keep enabled so missing node resolvers fail during schema build. Disable only during controlled migrations. |
| `MaxAllowedNodeBatchSize`     | `50`    | Match backend cost and request-limit policy.                                                                |
| `MarkNodeFieldAsLookup`       | `false` | Enable for Fusion source schema lookup metadata. See [Fusion](/docs/hotchocolate/v16/_leagcy/fusion).       |

# Expose global IDs on output fields

Use `[ID]` when the field belongs to the object type that owns the ID.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed class Product
{
    [ID]
    public int Id { get; init; }

    public required string Name { get; init; }
}
```

Expected SDL:

```graphql
type Product {
  id: ID!
  name: String!
}
```

Example response:

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "name": "Trail Backpack"
    }
  }
}
```

The response value is opaque. The example shows the default format for the examples on this page; do not write clients that depend on that string shape.

For descriptor-first configuration, use `.ID()`.

```csharp
#nullable enable

using HotChocolate.Types;

namespace Catalog.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(product => product.Id).ID();
    }
}
```

Use a typed ID when the field points at another object type, such as a foreign key.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed class OrderItem
{
    [ID]
    public int Id { get; init; }

    [ID<Product>]
    public int ProductId { get; init; }

    public int Quantity { get; init; }
}
```

Descriptor equivalent:

```csharp
descriptor.Field(item => item.ProductId).ID("Product");
```

| Goal                                                            | Attribute-first API                    | Descriptor-first API                                                |
| --------------------------------------------------------------- | -------------------------------------- | ------------------------------------------------------------------- |
| Mark an output ID as global                                     | `[ID]`                                 | `.ID()`                                                             |
| Mark a foreign key as another type's ID                         | `[ID<Product>]` or `[ID("Product")]`   | `.ID("Product")`                                                    |
| Accept a global ID argument                                     | `[ID<Product>] int id`                 | `.Argument("id", a => a.Type<NonNullType<IdType>>().ID("Product"))` |
| Make a type implement `Node`                                    | `[Node]`                               | `.ImplementsNode()`                                                 |
| Choose the Node ID field                                        | `[Node(IdField = nameof(ProductId))]`  | `.IdField(t => t.ProductId)`                                        |
| Configure node lookup                                           | `[NodeResolver]` or `NodeResolverType` | `.ResolveNode(...)` or `.ResolveNodeWith<T>()`                      |
| Use a plain GraphQL `ID` scalar without global ID serialization | `[GraphQLType<IdType>]`                | `.Type<IdType>()`                                                   |

A property named `Id` is not enough to create global ID behavior. Bind it as an ID field with `[ID]`, `.ID()`, or Node configuration.

# Accept global IDs in arguments and inputs

When a client sends a global ID back to your schema, annotate the argument or input field so Hot Chocolate parses it before your resolver runs. Your resolver receives the raw CLR value.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  productById(id: ID!): Product
}
```

Client query:

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

Use the same pattern on input object fields.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

public sealed class AddOrderItemInput
{
    [ID<Product>]
    public int ProductId { get; init; }

    public int Quantity { get; init; }
}
```

Descriptor-first argument configuration:

```csharp
descriptor
    .Field("productById")
    .Argument("id", argument => argument.Type<NonNullType<IdType>>().ID("Product"));
```

Type-restricted IDs reject mismatches. A `Brand` ID sent to `[ID<Product>] int id` fails before your resolver receives the value. Use plain `[ID]` only when the field can accept IDs from more than one GraphQL type.

# Make a type refetchable with Node

Use Node support when a client should refetch an entity by ID without knowing the original query path. A node type needs an ID field and a resolver that can load one object by raw ID.

The attribute-first path is to add `[Node]` and a conventional static resolver.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[Node]
public sealed class Product
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public static Task<Product?> GetAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

Expected SDL:

```graphql
type Product implements Node {
  id: ID!
  name: String!
}
```

`[Node]` configures the type to implement `Node` and turns the selected ID field into a global ID. The resolver receives `int id`, not the global ID string.

If your CLR member is not named `Id`, set `IdField`.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[Node(IdField = nameof(ProductId))]
public sealed class Product
{
    public int ProductId { get; init; }

    public required string Name { get; init; }

    public static Task<Product?> GetAsync(
        int productId,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(productId, cancellationToken);
}
```

Hot Chocolate can discover conventional resolver names such as `Get`, `GetAsync`, `GetProduct`, and `GetProductAsync`. Prefer explicit configuration when a resolver name or location could surprise future maintainers.

# Add a node resolver on an existing query field

If you already have a query field that loads an object by primary key, annotate it with `[NodeResolver]`. This reuses the same method for normal query access and node refetch.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[QueryType]
public static partial class ProductQueries
{
    [NodeResolver]
    public static Task<Product?> GetProductByIdAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

A node resolver should return a nullable object. `node(id:)` returns `null` when the object no longer exists or the resolver cannot load the value. If the ID contains the type name of another configured Node type, Hot Chocolate dispatches to that type's node resolver instead.

For production workloads, prefer a DataLoader or another batching strategy inside node resolvers. `nodes(ids:)` can request many IDs in one operation.

# Put node lookup in a separate resolver type

Use `NodeResolverType` when the entity class should stay free of resolver methods.

```csharp
#nullable enable

using HotChocolate.Types.Relay;

namespace Catalog.Types;

[Node(
    NodeResolverType = typeof(ProductNodeResolver),
    NodeResolver = nameof(ProductNodeResolver.GetProductByIdAsync))]
public sealed class Product
{
    public int Id { get; init; }

    public required string Name { get; init; }
}

public sealed class ProductNodeResolver
{
    public Task<Product?> GetProductByIdAsync(
        int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

Descriptor-first configuration gives the same schema shape.

```csharp
#nullable enable

using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace Catalog.Types;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(product => product.Id)
            .ResolveNode(async (context, id) =>
            {
                var products = context.Service<ProductService>();
                return await products.FindByIdAsync(id, context.RequestAborted);
            });
    }
}
```

Or point the descriptor at a resolver method.

```csharp
descriptor
    .ImplementsNode()
    .IdField(product => product.Id)
    .ResolveNodeWith<ProductNodeResolver>(
        resolver => resolver.GetProductByIdAsync(default, default!, default));
```

# Refetch objects with `node` and `nodes`

Use `node(id:)` when the client has one ID and needs the object.

```graphql
query GetNode($id: ID!) {
  node(id: $id) {
    id
    __typename
    ... on Product {
      name
    }
  }
}
```

Example variables:

```json
{
  "id": "UHJvZHVjdDox"
}
```

Example response:

```json
{
  "data": {
    "node": {
      "id": "UHJvZHVjdDox",
      "__typename": "Product",
      "name": "Trail Backpack"
    }
  }
}
```

Use `nodes(ids:)` when the client has several IDs. Select inline fragments because the static type is `Node`.

```graphql
query GetNodes($ids: [ID!]!) {
  nodes(ids: $ids) {
    id
    __typename
    ... on Product {
      name
    }
    ... on Brand {
      name
    }
  }
}
```

Example variables:

```json
{
  "ids": ["UHJvZHVjdDox", "QnJhbmQ6MQ=="]
}
```

Expected behavior:

| Lookup                         | Behavior                                                                                   |
| ------------------------------ | ------------------------------------------------------------------------------------------ |
| `node(id:)`                    | Returns a `Node` object or `null`.                                                         |
| `nodes(ids:)`                  | Returns a non-null list whose entries align with the requested IDs. Entries may be `null`. |
| Single value for `nodes(ids:)` | GraphQL list coercion accepts one ID string and treats it as a one-item list.              |
| Batch above limit              | Fails with a validation error. The default `MaxAllowedNodeBatchSize` is `50`.              |

# Choose type names deliberately

Global IDs include the GraphQL type name. In v16, `[ID<T>]` uses the configured GraphQL type name for `T` when Hot Chocolate can resolve it.

```csharp
#nullable enable

using HotChocolate;
using HotChocolate.Types.Relay;

namespace Catalog.Types;

[GraphQLName("Product")]
public sealed class ProductDto
{
    [ID<ProductDto>]
    public int Id { get; init; }

    public required string Name { get; init; }
}
```

The public type is `Product`, so the global ID is associated with `Product`, not `ProductDto`. If the target type cannot be expressed by a CLR type, or you are migrating from an older naming scheme, use a string name.

```csharp
public sealed class OrderItem
{
    [ID("Product")]
    public int ProductId { get; init; }
}
```

Changing a GraphQL type name can change newly emitted global IDs. Plan migrations before renaming public types, and keep clients from parsing ID strings.

# Customize ID serialization only when needed

Most applications should use the default serializer registered by `AddGlobalObjectIdentification()`. Change serializer configuration for migration compatibility, URL-safe route segments, maximum length requirements, or custom runtime ID value types.

Register serializer configuration before `AddGlobalObjectIdentification()`.

```csharp
builder
    .AddGraphQL()
    .AddLegacyNodeIdSerializer()
    .AddGlobalObjectIdentification();
```

Use the legacy serializer when you must continue emitting the pre-v14 format during a migration.

For staged migrations, parse the current format while emitting the older format until all services can parse the current one.

```csharp
builder
    .AddGraphQL()
    .AddDefaultNodeIdSerializer(outputNewIdFormat: false)
    .AddGlobalObjectIdentification();
```

Configure serializer options when you need a specific encoding format or length limit.

```csharp
#nullable enable

using HotChocolate.Execution.Options;
using HotChocolate.Execution.Relay;

builder
    .AddGraphQL()
    .AddDefaultNodeIdSerializer(new NodeIdSerializerOptions
    {
        Format = NodeIdSerializerFormat.UrlSafeBase64,
        MaxIdLength = 2048
    })
    .AddGlobalObjectIdentification();
```

If the encoding format matters, set `Format` explicitly. `NodeIdSerializerOptions` defaults `Format` to `UrlSafeBase64`, while the parameter overload used by the default registration uses standard `Base64` unless you pass `useUrlSafeBase64: true` or provide options.

| API                                 | Use when                                                 | Notes                                                                                                 |
| ----------------------------------- | -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| `AddDefaultNodeIdSerializer(...)`   | You need format, output-format, or length options.       | The parameterless global object identification setup registers a default serializer when none exists. |
| `AddLegacyNodeIdSerializer()`       | You need to emit and parse the legacy format.            | Call it before `AddGlobalObjectIdentification()`.                                                     |
| `INodeIdSerializer`                 | Low-level formatting or parsing is unavoidable.          | Framework annotations cover normal field, argument, and node flows.                                   |
| `INodeIdValueSerializer`            | A custom runtime ID value type needs formatting support. | Keep composite or domain ID details in [Complex IDs](./complex-ids).                                  |
| `AddNodeIdValueSerializer<T>()`     | You have implemented a custom value serializer.          | Register it before IDs are used in the schema.                                                        |
| `AddNodeIdValueSerializerFrom<T>()` | You use generated serializer support.                    | Requires `HotChocolate.Types.Analyzers`.                                                              |

# Keep composite IDs out of client code

Complex storage identity is compatible with global IDs, but model it as one server-side value. Do not make clients concatenate parts.

```csharp
public readonly record struct ProductVariantId(int ProductId, string Sku);
```

Use [Complex IDs](./complex-ids) for record struct IDs, type converters, `CompositeNodeIdValueSerializer<T>`, and generated node ID value serializers.

# Troubleshoot global IDs and nodes

| Symptom                                                     | Likely cause                                                                                                 | Fix                                                                                                                 |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| `id` is `Int`, not `ID`.                                    | Field inference produced a normal scalar field.                                                              | Add `[ID]`, `[Node]`, `[GraphQLType<IdType>]`, or descriptor `.ID()` based on the behavior you need.                |
| A resolver receives a string instead of an `int` or `Guid`. | The argument or input field was not configured as a global ID.                                               | Add `[ID]`, `[ID<Product>]`, `[ID("Product")]`, or descriptor `.ID("Product")`.                                     |
| A `Brand` ID is accepted where a `Product` ID was expected. | The field uses plain `[ID]` with no target type restriction.                                                 | Use `[ID<Product>]`, `[ID("Product")]`, or `.ID("Product")`.                                                        |
| Schema build fails after adding Node.                       | `EnsureAllNodesCanBeResolved` is enabled and Hot Chocolate cannot find a node resolver.                      | Add a conventional resolver, `[NodeResolver]`, `NodeResolverType`, `.ResolveNode(...)`, or `.ResolveNodeWith<T>()`. |
| `node(id:)` returns `null`.                                 | The object no longer exists, the ID type name is unknown or not resolvable, or the resolver returned `null`. | Verify the ID type name, the node configuration, and the resolver lookup.                                           |
| `nodes(ids:)` fails with code `HC0076`.                     | The request exceeds `MaxAllowedNodeBatchSize`.                                                               | Request fewer IDs, page at the client, or change the option based on backend cost.                                  |
| IDs changed after an upgrade or type rename.                | Serializer format or GraphQL type-name behavior changed.                                                     | Use explicit type names, stage serializer migration, and review the v13 to v14 and v15 to v16 migration notes.      |
| A normalized client reports inconsistent `__typename`.      | Public IDs are not stable across object types, or clients parse and rewrite IDs.                             | Use global IDs, return `__typename` where the client needs it, and treat IDs as opaque.                             |

# Next steps

| If you need to...                                      | Go to                                                                                                                                                         |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Review the Relay schema conventions together.          | [Relay](./)                                                                                                                                                   |
| Page through large lists with cursors.                 | [Connections](./connections)                                                                                                                                  |
| Model tenant-local, SKU-based, or other composite IDs. | [Complex IDs](./complex-ids)                                                                                                                                  |
| Learn how resolver parameters and services are bound.  | [Resolvers](/docs/hotchocolate/v16/build/resolvers)                                                                                                           |
| Batch database access in node resolvers.               | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                                         |
| Tune request limits for node lookup.                   | [Request Limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits)                                                                            |
| Understand the plain `ID` scalar.                      | [Scalars](../scalars)                                                                                                                                         |
| Migrate ID formats or v16 `[ID<T>]` behavior.          | [v13 to v14](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-13-to-14) and [v15 to v16](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16) |
