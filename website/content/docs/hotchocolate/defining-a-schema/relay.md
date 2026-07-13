---
title: "Relay"
description: "Add global IDs, Node refetching, and query fields on mutation payloads to a Hot Chocolate schema."
---

The [GraphQL Global Object Identification Specification](https://relay.dev/graphql/objectidentification.htm) defines patterns for stable object identification and refetching. These patterns work with every GraphQL client, not only Relay.

This page covers three related features:

- Global IDs expose type-aware, opaque IDs to clients while your application continues to use the original ID values.
- Global object identification adds the `Node` interface and the `node` and `nodes` query fields.
- Mutation payload query fields let a mutation response read from the Query root.

You enable global ID formatting through `AddGlobalObjectIdentification`. Pass `false` when you need global IDs without the `Node` interface and its query fields. You enable mutation payload query fields separately. For Relay-style connections and cursors, see [Pagination](../fetching-data/pagination.md).

# Before You Begin

Install `HotChocolate.AspNetCore` and register a GraphQL server. The examples use these namespaces:

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
```

The data-access examples also assume that your application registers an Entity Framework Core `CatalogContext` with a `Products` set.

Enable global ID encoding and decoding before you apply `[ID]` or `.ID()`. If you do not need Node refetching, disable registration of the `Node` interface:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification(registerNodeInterface: false);
```

This registration adds the default node ID serializer and enables the formatters that encode and decode global IDs. The later Node section shows how to enable object refetching instead.

> [!NOTE]
> The GraphQL [`ID` scalar](./scalars.md#id) does not make a value globally unique. Hot Chocolate global IDs add the GraphQL type name to a stable ID that is already unique within its type. Clients must treat the resulting value as opaque.

# Expose Global IDs

Hot Chocolate formats a global ID from the GraphQL type name and the raw CLR ID. With the default serializer registration, the external representation uses standard Base64. Other serializer configurations can use URL-safe Base64, hexadecimal, or Base36, so clients must not decode or construct IDs themselves.

## Format Output Fields

Apply `[ID]` to an output property:

<ExampleTabs>
<Implementation>

```csharp
public class Product
{
    [ID]
    public int Id { get; set; }

    public required string Name { get; set; }
}
```

For this non-nullable `int`, the schema field becomes `id: ID!`. The ID formatter preserves the original nullability and list wrappers. For example, a nullable `int?` becomes `ID`, not `ID!`.

By default, the owning GraphQL type supplies the type name. For a foreign key, specify the referenced type:

```csharp
public class OrderItem
{
    [ID]
    public int Id { get; set; }

    [ID<Product>]
    public int ProductId { get; set; }
}
```

`[ID<Product>]` resolves the configured GraphQL name for `Product`. You can pin a literal GraphQL type name with `[ID("Product")]`.

</Implementation>
<Code>

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(product => product.Id).ID();
    }
}
```

For a foreign key on an `OrderItemType`, specify the referenced type name:

```csharp
descriptor.Field(item => item.ProductId).ID("Product");
```

</Code>
</ExampleTabs>

If `Product.Id` is `1`, the default serializer currently returns `UHJvZHVjdDox`. This value is an implementation detail. Store and return it without interpreting it.

## Decode IDs in Arguments

Mark every argument that accepts a global ID with `[ID]`. Hot Chocolate decodes the external value before it calls your resolver, so `id` remains an `int` in your business code.

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [ID<Product>] int id,
        CatalogContext db)
        => db.Products.Find(id);

    public static Product? GetFeaturedProduct(CatalogContext db)
        => db.Products.Find(1);
}
```

Use `[ID]` when the resolver accepts any global ID type. Use `[ID<Product>]` or `[ID("Product")]` when it must reject IDs created for another GraphQL type.

</Implementation>
<Code>

```csharp
descriptor
    .Field("product")
    .Argument("id", argument =>
        argument.Type<NonNullType<IdType>>().ID(nameof(Product)))
    .Type<ProductType>()
    .Resolve(context =>
    {
        var id = context.ArgumentValue<int>("id");
        // Load and return the product.
    });
```

Omit the type-name argument from `.ID()` when the resolver accepts any global ID type.

</Code>
</ExampleTabs>

For more argument patterns, see [Arguments](./arguments.md).

## Decode IDs in Input Objects

Apply `[ID]` to an input object property to decode the external value during input coercion:

```csharp
public class UpdateProductInput
{
    [ID<Product>]
    public int ProductId { get; set; }

    public required string Name { get; set; }
}
```

For a record primary-constructor parameter, target the generated property:

```csharp
public sealed record UpdateProductInput(
    [property: ID<Product>] int ProductId,
    string Name);
```

## Format an ID in Custom Code

Inject `INodeIdSerializer` when application code needs to format an ID outside an ID field. The GraphQL type and its ID runtime type must be part of the built schema so the schema-scoped serializer can bind them.

```csharp
[QueryType]
public static partial class ProductIdQueries
{
    public static string GetGlobalProductId(
        int productId,
        INodeIdSerializer serializer)
        => serializer.Format("Product", productId);
}
```

`Format` accepts the GraphQL type name and raw ID. Parsing has a different contract: `Parse` also requires the target runtime type or an `INodeIdRuntimeTypeLookup`, and it returns a `NodeId` containing `TypeName` and `InternalId`.

# Refetch Objects Through `node`

To add the `Node` interface and the `node` and `nodes` query fields, use the default overload instead of the ID-only registration:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification();
```

This registration adds the following schema types and fields:

```graphql
interface Node {
  id: ID!
}

type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

The `node` result is nullable because an ID might not resolve to an object. The `nodes` field and its list are non-null, while individual list items can be null.

To make an object refetchable through `node`, configure all three parts of the contract:

1. The object implements `Node`.
2. It exposes `id: ID!`.
3. It has a node resolver that loads one object from its raw ID.

At least one object type must implement `Node`, or strict schema validation fails. By default, schema validation also rejects each object that implements `Node` without a node resolver. Set `EnsureAllNodesCanBeResolved` to `false` only when unresolved Node types are intentional.

## Implement a Node with Attributes

Apply `[Node]` to the object. Hot Chocolate looks for a public static resolver named `Get`, `GetAsync`, `Get{TypeName}`, or `Get{TypeName}Async`.

```csharp
[Node]
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public static async Task<Product?> GetAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

`[Node]` makes `Product` implement `Node`, exposes its `Id` property as a global `id: ID!` field, and uses `GetAsync` for node lookup.

If the CLR property has another name, select it explicitly:

```csharp
[Node(IdField = nameof(ProductId))]
public class Product
{
    public int ProductId { get; set; }
    public required string Name { get; set; }

    public static Product? Get(int id, CatalogContext db)
        => db.Products.Find(id);
}
```

The selected CLR member is exposed as `id` to satisfy the `Node` interface.

To select a resolver that does not follow the naming convention, apply `[NodeResolver]`:

```csharp
[NodeResolver]
public static async Task<Product?> FetchByIdAsync(
    int id,
    CatalogContext db,
    CancellationToken ct)
    => await db.Products.FindAsync([id], ct);
```

A node resolver must be public. Its first field argument must be named `id` or end in `Id`, and a node resolver can have only one field argument. Services, resolver context, and `CancellationToken` parameters do not count as field arguments. Do not add `[ID]` to the ID parameter because the node resolver already configures it as an ID.

## Put the Node Resolver in Another Class

Point `[Node]` at a separate resolver type when you do not want data access on the model:

```csharp
[Node(
    NodeResolverType = typeof(ProductNodeResolver),
    NodeResolver = nameof(ProductNodeResolver.GetProductAsync))]
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public sealed class ProductNodeResolver
{
    public async Task<Product?> GetProductAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

## Implement a Node with Descriptors

Use `.ImplementsNode()` when you configure object types with descriptors:

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(product => product.Id)
            .ResolveNode(async (context, id) =>
            {
                var db = context.Service<CatalogContext>();
                return await db.Products.FindAsync([id]);
            });
    }
}
```

To use the separate resolver class shown above, identify its full method signature in the expression:

```csharp
descriptor
    .ImplementsNode()
    .IdField(product => product.Id)
    .ResolveNodeWith<ProductNodeResolver>(resolver =>
        resolver.GetProductAsync(default, default!, default));
```

Node resolvers are a good place to use [DataLoaders](../fetching-data/batching/dataloader.md) when repeated lookups should be batched or cached.

## Add Node Support Through a Type Extension

When you add Node support through a type extension, place `[Node]` on the extension class:

```csharp
[Node]
[ExtendObjectType<Product>]
public static partial class ProductExtensions
{
    public static async Task<Product?> GetAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

This static partial extension requires `HotChocolate.Types.Analyzers`. Register it through the generated type module. Projects created from the Hot Chocolate server template do this by calling `.AddTypes()` during server registration.

## Refetch a Node End to End

Assume the database contains a product with raw ID `1` and name `Green Tea`. First, query a regular field and save the returned global ID:

```graphql
query GetFeaturedProduct {
  featuredProduct {
    id
    name
  }
}
```

With the default serializer settings, the response is:

```json
{
  "data": {
    "featuredProduct": {
      "id": "UHJvZHVjdDox",
      "name": "Green Tea"
    }
  }
}
```

Pass that opaque ID to `node`:

```graphql
query RefetchProduct($id: ID!) {
  node(id: $id) {
    id
    ... on Product {
      name
    }
  }
}
```

**Variables**

```json
{
  "id": "UHJvZHVjdDox"
}
```

The response resolves the same product:

**Response**

```json
{
  "data": {
    "node": {
      "id": "UHJvZHVjdDox",
      "name": "Green Tea"
    }
  }
}
```

The client uses the global ID, while `Product.GetAsync` receives the raw `int` value `1`.

## Limit Batch Node Lookups

The `nodes` field accepts a list of IDs. Limit the number of IDs that a single request can resolve:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification(options =>
    {
        options.MaxAllowedNodeBatchSize = 50;
    });
```

The default limit is `50`. Requests above the configured limit fail with error code `HC0076`. See [Global object identification options](../server/options.md#global-object-identification-options) for all options and [Nodes batch size](../security/request-limits.md#nodes-batch-size) for security guidance.

# Use Composite CLR IDs

The built-in node ID serializers support common scalar CLR types. For a composite ID, use the `HotChocolate.Types.Analyzers` source generator to create and register an `INodeIdValueSerializer` implementation.

Define the ID as a record struct whose components are public readable properties:

```csharp
public readonly record struct ProductId(string Sku, int BatchNumber);

public class Product
{
    [ID]
    public ProductId Id { get; set; }

    public required string Name { get; set; }
}
```

The Hot Chocolate server template already references `HotChocolate.Types.Analyzers`. If your project does not, add the package with the same version as your other Hot Chocolate packages. Replace `<VERSION>` with that version:

```bash
dotnet add package HotChocolate.Types.Analyzers --version <VERSION>
```

Then call the generator activation method in your server registration:

```csharp
builder
    .AddGraphQL()
    .AddNodeIdValueSerializerFrom<ProductId>()
    .AddGlobalObjectIdentification(registerNodeInterface: false);
```

The call generates and registers a serializer for `ProductId`. The generated serializer supports components of type `string`, `short`, `int`, `long`, `bool`, and `Guid`. Use public readable instance properties. A positional record or a type with a matching constructor can use read-only properties. Other supported types need assignable properties.

Every property that participates in identity must use one of the supported types. The generator ignores unsupported properties. Implement a custom serializer when any identity component uses another type. General Hot Chocolate type converters do not register a node ID value serializer.

If the generated shape does not fit your ID type, implement `INodeIdValueSerializer` or derive from `CompositeNodeIdValueSerializer<T>`, then register it with `.AddNodeIdValueSerializer<TSerializer>()`.

The following resolver exposes the decoded components so you can verify the round trip:

```csharp
[QueryType]
public static partial class CompositeProductQueries
{
    public static Product GetCompositeProduct(
        [ID<Product>] ProductId id)
        => new()
        {
            Id = id,
            Name = $"{id.Sku}/{id.BatchNumber}"
        };
}
```

With the default serializer, `ProductId("ABC", 42)` is represented by `UHJvZHVjdDpBQkM6NDI=`. Pass that value back without decoding or constructing it in client code:

```graphql
query GetCompositeProduct($id: ID!) {
  compositeProduct(id: $id) {
    id
    name
  }
}
```

**Variables**

```json
{
  "id": "UHJvZHVjdDpBQkM6NDI="
}
```

**Response**

```json
{
  "data": {
    "compositeProduct": {
      "id": "UHJvZHVjdDpBQkM6NDI=",
      "name": "ABC/42"
    }
  }
}
```

# Query from a Mutation Payload

Mutation payload query fields are independent of global IDs and the `node` field. Enable them separately:

```csharp
builder
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads();
```

By default, Hot Chocolate adds `query: Query!` to object types returned by mutation fields whose names end in `Payload`. The field points to your configured Query root, even when that root has another GraphQL name.

For example, define a mutation payload:

```csharp
[MutationType]
public static partial class ProductMutations
{
    public static async Task<RenameProductPayload> RenameProductAsync(
        [ID<Product>] int id,
        string name,
        CatalogContext db,
        CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new GraphQLException("Product not found.");

        product.Name = name;
        await db.SaveChangesAsync(ct);
        return new RenameProductPayload(product);
    }
}

public sealed record RenameProductPayload(Product Product);
```

The payload includes the added field:

```graphql
type RenameProductPayload {
  product: Product!
  query: Query!
}
```

You can return the changed object and refetch other query data in the same request:

```graphql
mutation RenameProduct($id: ID!) {
  renameProduct(id: $id, name: "Green Tea") {
    product {
      id
      name
    }
    query {
      product(id: $id) {
        name
      }
    }
  }
}
```

**Variables**

```json
{
  "id": "UHJvZHVjdDox"
}
```

**Response**

```json
{
  "data": {
    "renameProduct": {
      "product": {
        "id": "UHJvZHVjdDox",
        "name": "Green Tea"
      },
      "query": {
        "product": {
          "name": "Green Tea"
        }
      }
    }
  }
}
```

Customize the field name and the payload-type predicate when your naming convention differs:

```csharp
builder
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
        options.MutationPayloadPredicate = type =>
            type.Name.EndsWith("Result", StringComparison.Ordinal);
    });
```

This configuration adds `rootQuery: Query!` only to matching mutation result types.

# Troubleshooting

## `HotChocolate.Types.Analyzers is required to use this method.`

Your application called `AddNodeIdValueSerializerFrom<T>()` without the analyzer that intercepts the call. Reference `HotChocolate.Types.Analyzers` with the same version as the server packages, then rebuild the project.

## No serializer registered for type `ProductId`.

The default serializers do not know how to format your custom CLR ID. Generate one with `AddNodeIdValueSerializerFrom<T>()`, or register a custom implementation with `AddNodeIdValueSerializer<TSerializer>()`. General type converters do not participate in node ID formatting.

## The Schema Rejects a Type That Implements `Node`

Each Node object needs a node resolver when `EnsureAllNodesCanBeResolved` is enabled, which is the default. Add a resolver through `[Node]`, `[NodeResolver]`, `.ResolveNode(...)`, or `.ResolveNodeWith(...)`.

If the type intentionally cannot be refetched, reconsider whether it should implement `Node`. You can opt out of validation with `EnsureAllNodesCanBeResolved = false`, but `node` cannot resolve that type. See [Global object identification options](../server/options.md#global-object-identification-options).

## A Node Resolver Produces an Analyzer Error

Make the resolver public and name its first field argument `id` or use a name that ends in `Id`. Do not annotate that argument with `[ID]`. If an ID appears on a record primary-constructor parameter, target the property with `[property: ID]`.

# Next Steps

- Use [DataLoader](../fetching-data/batching/dataloader.md) to batch and cache node lookups.
- Add Relay-style connections with [Pagination](../fetching-data/pagination.md).
- Review the [`ID` scalar](./scalars.md#id) and other [Scalars](./scalars.md).
- Configure schema objects with [Object Types](./object-types.md).
- For distributed schemas, see [GraphQL global object identification in Fusion](../../fusion/entities-and-lookups.md#graphql-global-object-identification).
