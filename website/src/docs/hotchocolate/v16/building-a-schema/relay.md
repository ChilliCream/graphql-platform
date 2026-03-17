---
title: "Relay"
---

The Relay GraphQL Server Specification defines patterns for globally unique identifiers, object refetching, and cursor-based pagination. While these patterns originated in Facebook's Relay client, they improve schema design for any GraphQL client.

> Note: The patterns on this page benefit all GraphQL clients, not only Relay. We recommend them for every Hot Chocolate project.

# Global Identifiers

GraphQL clients often use the `id` field to build a client-side cache. If two different types both have a row with `id: 1`, the cache encounters collisions. Global identifiers solve this by encoding the type name and the underlying ID into an opaque, Base64-encoded string that is unique across the entire schema.

Hot Chocolate handles this through a middleware. The `[ID]` attribute opts a field into global identifier behavior. At runtime, Hot Chocolate combines the type name with the raw ID to produce a globally unique value. Your business code continues to work with the original ID.

## Output Fields

<ExampleTabs>
<Implementation>

```csharp
// Types/Product.cs
public class Product
{
    [ID]
    public int Id { get; set; }

    public string Name { get; set; }
}
```

The `[ID]` attribute rewrites the field type to `ID!` and serializes the value as a global identifier. By default, it uses the owning type name (`Product`) for serialization.

For foreign key fields that reference another type, specify the target type name:

```csharp
// Types/OrderItem.cs
public class OrderItem
{
    [ID]
    public int Id { get; set; }

    [ID<Product>]
    public int ProductId { get; set; }
}
```

In v16, the generic `[ID<Product>]` form infers the GraphQL type name from the type argument. You can also use `[ID("Product")]` to specify it as a string.

</Implementation>
<Code>

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(f => f.Id).ID();
    }
}
```

For foreign key fields:

```csharp
descriptor.Field(f => f.ProductId).ID("Product");
```

</Code>
</ExampleTabs>

## Input Arguments

When a field returns a serialized global ID, any argument that accepts that ID must also be marked with `[ID]` to deserialize it back to the raw value.

<ExampleTabs>
<Implementation>

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [ID] int id,
        CatalogContext db)
        => db.Products.Find(id);
}
```

To restrict the argument to IDs serialized for a specific type:

```csharp
public static Product? GetProduct(
    [ID<Product>] int id,
    CatalogContext db)
    => db.Products.Find(id);
```

This rejects IDs that were serialized for a different type.

</Implementation>
<Code>

```csharp
descriptor
    .Field("product")
    .Argument("id", a => a.Type<NonNullType<IdType>>().ID())
    .Type<ProductType>()
    .Resolve(context =>
    {
        var id = context.ArgumentValue<int>("id");
        // ...
    });
```

To restrict to a specific type:

```csharp
.Argument("id", a => a.Type<NonNullType<IdType>>().ID(nameof(Product)))
```

</Code>
</ExampleTabs>

## Input Object Fields

Mark input object properties with `[ID]` to deserialize global IDs in input types.

```csharp
// Types/UpdateProductInput.cs
public class UpdateProductInput
{
    [ID]
    public int ProductId { get; set; }

    public string Name { get; set; }
}
```

## ID Serializer

You can access the `IIdSerializer` service directly to serialize or deserialize global IDs in custom code.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static string GetGlobalId(int productId, IIdSerializer serializer)
    {
        return serializer.Serialize(null, "Product", productId);
    }
}
```

The `Serialize` method takes the schema name (or `null` for the default schema), the type name, and the raw ID.

# Global Object Identification

Global object identification extends global identifiers by enabling clients to refetch any object by its ID through a standardized `node` query field. This requires three things:

1. The type implements the `Node` interface.
2. The type has an `id: ID!` field.
3. A node resolver method can fetch the object by its ID.

## Enabling Global Object Identification

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification();
```

This adds the `Node` interface and the `node` / `nodes` query fields:

```graphql
interface Node {
  id: ID!
}

type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

You can configure options when enabling global object identification:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification(opts =>
    {
        opts.MaxAllowedNodeBatchSize = 50;
    });
```

At least one type in the schema must implement `Node`, or the schema fails to build.

## Implementing Node

<ExampleTabs>
<Implementation>

Annotate your class with `[Node]`. Hot Chocolate looks for a static method named `Get`, `GetAsync`, `Get{TypeName}`, or `Get{TypeName}Async` that accepts the ID as its first parameter and returns the type.

```csharp
// Types/Product.cs
[Node]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }

    public static async Task<Product?> GetAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

The `[Node]` attribute causes the type to implement the `Node` interface and turns the `Id` property into a global identifier.

If your ID property is not named `Id`, specify it:

```csharp
[Node(IdField = nameof(ProductId))]
public class Product
{
    public int ProductId { get; set; }
    // ...
}
```

If your resolver method does not follow the naming convention, annotate it with `[NodeResolver]`:

```csharp
[NodeResolver]
public static async Task<Product?> FetchByIdAsync(int id, CatalogContext db, CancellationToken ct)
    => await db.Products.FindAsync([id], ct);
```

To place the node resolver in a separate class:

```csharp
[Node(
    NodeResolverType = typeof(ProductNodeResolver),
    NodeResolver = nameof(ProductNodeResolver.GetProductAsync))]
public class Product
{
    public int Id { get; set; }
}

public class ProductNodeResolver
{
    public static async Task<Product?> GetProductAsync(
        int id, CatalogContext db, CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

</Implementation>
<Code>

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) =>
            {
                var db = context.Service<CatalogContext>();
                return await db.Products.FindAsync([id]);
            });
    }
}
```

If the ID property is not named `Id`, specify it with `IdField`. Hot Chocolate renames it to `id` in the schema to satisfy the `Node` interface contract.

To resolve using a separate class:

```csharp
descriptor
    .ImplementsNode()
    .IdField(f => f.ProductId)
    .ResolveNodeWith<ProductNodeResolver>(r => r.GetProductAsync(default!));
```

</Code>
</ExampleTabs>

Node resolvers are ideal places to use [DataLoaders](/docs/hotchocolate/v16/fetching-data/dataloader) for efficient batched fetching.

## Node with Type Extensions

When adding Node support through a type extension, place the `[Node]` attribute on the extension class:

```csharp
// Types/ProductExtensions.cs
[Node]
[ExtendObjectType<Product>]
public static partial class ProductExtensions
{
    public static async Task<Product?> GetAsync(
        int id, CatalogContext db, CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

# Complex IDs

Some data models use composite keys (multiple fields forming a unique identifier). Hot Chocolate supports complex IDs through custom ID types and type converters.

```csharp
// Types/ProductId.cs
public readonly record struct ProductId(string Sku, int BatchNumber)
{
    public override string ToString() => $"{Sku}:{BatchNumber}";

    public static ProductId Parse(string value)
    {
        var parts = value.Split(':');
        return new ProductId(parts[0], int.Parse(parts[1]));
    }
}

// Types/Product.cs
public class Product
{
    [ID]
    public ProductId Id { get; set; }
}
```

Register type converters so Hot Chocolate can serialize and deserialize the complex ID:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeConverter<string, ProductId>(ProductId.Parse)
    .AddTypeConverter<ProductId, string>(x => x.ToString())
    .AddGlobalObjectIdentification();
```

In v16, the source generator can produce a `NodeIdValueSerializer` for your custom ID type, reducing the need for manual converter registration.

# Query Field in Mutation Payloads

Mutation payloads can include a `query` field that gives clients access to the full Query type. This lets a client fetch everything it needs to update its state in a single round trip.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryFieldToMutationPayloads();
```

By default, a `query: Query` field is added to every mutation payload type whose name ends in `Payload`. You can customize this:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
        options.MutationPayloadPredicate =
            (type) => type.Name.Value.EndsWith("Result");
    });
```

# Troubleshooting

## "No type implements Node"

At least one type must implement the `Node` interface when `AddGlobalObjectIdentification` is called. Annotate a type with `[Node]` or use `ImplementsNode()` in code-first.

## Node resolver not found

Hot Chocolate looks for a static method named `Get`, `GetAsync`, `Get{TypeName}`, or `Get{TypeName}Async`. If your method uses a different name, annotate it with `[NodeResolver]`.

## Global ID deserialization fails

If a client sends an ID that was serialized for a different type, deserialization fails with a type mismatch error. Verify the `[ID]` type name matches between the field that produced the ID and the argument that consumes it.

## MaxAllowedNodeBatchSize

In v16, `MaxAllowedNodeBatchSize` has moved from the `Node` type configuration to the `AddGlobalObjectIdentification` options. Pass it when calling `AddGlobalObjectIdentification(opts => opts.MaxAllowedNodeBatchSize = 50)`.

# Next Steps

- **Need to fetch data efficiently?** See [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader).
- **Need pagination?** See [Pagination](/docs/hotchocolate/v16/fetching-data/pagination).
- **Need to understand ID types?** See [Scalars](/docs/hotchocolate/v16/defining-a-schema/scalars).
- **Need to extend types?** See [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types).
