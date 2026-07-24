---
title: "Relay"
description: "Give objects global IDs and let clients refetch them through the Relay node field."
---

A `Product` and an `Order` can both have the ID `1`. The value `1` alone does not tell a client which object it belongs to. This can cause collisions in a client cache.

Hot Chocolate solves this problem with a global ID. The server combines the object type and its ID into one opaque string. Opaque means that the client stores and returns the string without trying to read it. Your application still works with the original ID.

By the end of this guide, you will return a global ID for a product and use that ID to fetch the same product through the `node` query field.

These patterns follow the [GraphQL Global Object Identification Specification](https://relay.dev/graphql/objectidentification.htm). They work with every GraphQL client, not only Relay. For Relay-style connections and cursors, see [Pagination](../fetching-data/pagination.md).

# Before You Begin

Start with a running Hot Chocolate server created from the server template. If you do not have one, complete [Getting Started](../get-started-with-graphql-in-net-core.md) first.

The examples use an Entity Framework Core `CatalogContext` with a `Products` set. See [Entity Framework](../fetching-data/integrations/entity-framework.md) for database registration. The examples assume the database contains this product:

| ID  | Name      |
| --- | --------- |
| 1   | Green Tea |

The examples use these namespaces:

```csharp
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
```

# Add Global Object Identification

Add global object identification to the existing server registration. Keep the template's generated `.AddTypes()` call:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification()
    .AddTypes();
```

This enables global ID encoding and decoding. It also adds the `Node` interface and the `node` and `nodes` fields to Query.

# Make Product Refetchable

Add `[Node]` to `Product` and provide a method that loads one product by ID:

```csharp
[Node]
public sealed class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public static async Task<Product?> GetAsync(
        int id,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Products.FindAsync([id], cancellationToken);
}
```

`[Node]` does three things:

1. It makes `Product` implement the GraphQL `Node` interface.
2. It exposes `Product.Id` as a global `ID!` field.
3. It uses `GetAsync` when a client fetches a product through `node`.

Next, add a regular query field that returns the sample product:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetFeaturedProduct(CatalogContext db)
        => db.Products.Find(1);
}
```

If your setup is correct, the schema contains these fields:

```graphql
interface Node {
  id: ID!
}

type Product implements Node {
  id: ID!
  name: String!
}

type Query {
  featuredProduct: Product
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

The `node` field fetches one object. The `nodes` field fetches several objects in one request.

# Get a Global ID

Query the regular `featuredProduct` field:

```graphql
query GetFeaturedProduct {
  featuredProduct {
    id
    name
  }
}
```

With the default serializer, the response is:

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

The product still has the ID `1` in your application. Hot Chocolate can read `UHJvZHVjdDox` and determine that it refers to a `Product`.

The default serializer uses Base64. Other configurations can produce a different string, so clients must treat every global ID as opaque.

# Refetch the Product Through `node`

Pass the returned ID to the `node` field:

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

Hot Chocolate reads the global ID, finds the `Product` type, and calls `Product.GetAsync` with the original integer `1`. If no product has that ID, `node` returns `null`.

# Accept a Global ID as Input

Add a query field that accepts the global Product ID returned above:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [ID<Product>] int id,
        CatalogContext db)
        => db.Products.Find(id);
}
```

This declaration can live beside the earlier `ProductQueries` declaration because both are `partial`.

On an argument or input property, `[ID<Product>]` checks that the supplied global ID belongs to `Product`. Hot Chocolate then passes the original integer to your code. A global ID for another type is rejected before the resolver runs.

The field appears in the schema as:

```graphql
type Query {
  product(id: ID!): Product
}
```

# Expose Other Values as Global IDs

The GraphQL [`ID` scalar](./scalars.md#id) is not automatically a global ID. Add `[ID]` only when you want Hot Chocolate to encode a value as a global ID.

For an object's own ID, use `[ID]`. For a field that points to another object type, supply that type to the attribute:

```csharp
public sealed class OrderItem
{
    [ID]
    public int Id { get; set; }

    [ID<Product>]
    public int ProductId { get; set; }
}
```

On this output type, `[ID]` uses `OrderItem` as the type name for `id`. `[ID<Product>]` uses `Product` as the type name for `productId`.

You can apply the typed attribute to an input property too:

```csharp
public sealed class UpdateProductInput
{
    [ID<Product>]
    public int ProductId { get; set; }

    public required string Name { get; set; }
}
```

On input, the attribute checks that the ID belongs to `Product` and then supplies the original integer value. Hot Chocolate also preserves nullability. For example, `[ID] int` becomes `ID!`, while `[ID] int?` becomes `ID`.

For more argument and input examples, see [Arguments](./arguments.md).

# Optional: Query Updated Data from a Mutation

Mutation payload querying is a separate feature. It lets a client change data and read the updated state through top-level Query fields in one request.

Add it to the registration from the main tutorial:

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification()
    .AddQueryFieldToMutationPayloads()
    .AddTypes();
```

Hot Chocolate adds `query: Query!` to mutation result types whose names end in `Payload`. For example:

```csharp
[MutationType]
public static partial class ProductMutations
{
    public static async Task<RenameProductPayload> RenameProductAsync(
        [ID<Product>] int id,
        string name,
        CatalogContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products.FindAsync([id], cancellationToken)
            ?? throw new GraphQLException("Product not found.");

        product.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return new RenameProductPayload(product);
    }
}

public sealed record RenameProductPayload(Product Product);
```

The client can return the changed product and query it again from the same payload:

```graphql
mutation RenameProduct($id: ID!) {
  renameProduct(id: $id, name: "Black Tea") {
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
        "name": "Black Tea"
      },
      "query": {
        "product": {
          "name": "Black Tea"
        }
      }
    }
  }
}
```

# Advanced: Use a Composite ID

This section is an alternative to the integer ID used in the main tutorial. Do not apply it if your Product uses a single integer ID.

Assume that Entity Framework is configured to use both `Sku` and `BatchNumber` as the Product key. Define an ID type with those two parts:

```csharp
public readonly record struct ProductId(string Sku, int BatchNumber);
```

Register a generated serializer for this ID type before global object identification:

```csharp
builder
    .AddGraphQL()
    .AddNodeIdValueSerializerFrom<ProductId>()
    .AddGlobalObjectIdentification()
    .AddTypes();
```

Then replace the integer ID and resolvers from the main tutorial with these versions:

```csharp
[Node]
public sealed class Product
{
    public ProductId Id { get; set; }

    public required string Name { get; set; }

    public static async Task<Product?> GetAsync(
        ProductId id,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Products.FindAsync([id.Sku, id.BatchNumber], cancellationToken);
}

[QueryType]
public static partial class ProductQueries
{
    public static Product? GetFeaturedProduct(CatalogContext db)
        => db.Products.Find("ABC", 42);

    public static Product? GetProduct(
        [ID<Product>] ProductId id,
        CatalogContext db)
        => db.Products.Find(id.Sku, id.BatchNumber);
}
```

`AddNodeIdValueSerializerFrom<T>()` needs the `HotChocolate.Types.Analyzers` package. The Hot Chocolate server template already includes it. If your project does not, add the package with the same version as your other Hot Chocolate packages:

```bash
dotnet add package HotChocolate.Types.Analyzers --version <VERSION>
```

With the default serializer, a Product ID containing `"ABC"` and `42` is returned as:

```json
{
  "data": {
    "featuredProduct": {
      "id": "UHJvZHVjdDpBQkM6NDI=",
      "name": "Green Tea"
    }
  }
}
```

Clients must still treat this value as opaque. The generated serializer supports ID parts of type `string`, `short`, `int`, `long`, `bool`, and `Guid`. If your ID uses other part types, implement `INodeIdValueSerializer` and register it with `AddNodeIdValueSerializer<TSerializer>()`.

# Troubleshooting

## Attributed Fields Are Missing from the Schema

Keep the generated `.AddTypes()` call from the server template. It registers types marked with attributes such as `[QueryType]` and `[MutationType]`.

## `There is no object type implementing interface Node.`

`AddGlobalObjectIdentification()` adds the `Node` interface. Add `[Node]` to at least one object type and give that type a node resolver.

## A Node Type Has No Resolver

Every Node type needs a public static method that can load one object by ID. Hot Chocolate recognizes `Get`, `GetAsync`, `Get{TypeName}`, and `Get{TypeName}Async`. Use `[NodeResolver]` if the method has another name.

## `HotChocolate.Types.Analyzers is required to use this method.`

Your project called `AddNodeIdValueSerializerFrom<T>()` without the build-time generator. Add `HotChocolate.Types.Analyzers` with the same version as your other Hot Chocolate packages, then rebuild.

# Next Steps

- Use [DataLoader](../fetching-data/batching/dataloader.md) to batch or cache repeated node lookups.
- Review [global object identification options](../server/options.md#global-object-identification-options), including the `nodes` batch limit.
- Add Relay-style connections with [Pagination](../fetching-data/pagination.md).
- For distributed schemas, see [GraphQL global object identification in Fusion](../../fusion/entities-and-lookups.md#graphql-global-object-identification).
