---
title: "Entities and Lookups"
---

# Entities and Lookups

Entities are the mechanism that makes distributed GraphQL work. They are types that can be uniquely identified and resolved across multiple subgraphs -- the `Product` that the Products subgraph defines and the Reviews subgraph extends, the `User` that the Accounts subgraph owns and the Reviews subgraph adds reviews to. Without entities, each subgraph would be an isolated API. With them, you get one unified graph.

This page covers entity resolution in depth: how entities work, how lookups enable cross-subgraph resolution, how field ownership works, and how to optimize entity fetching. If you completed the [Getting Started](/docs/fusion/v16/getting-started) tutorial, you already used entities and lookups. This page goes deeper.

## What Makes a Type an Entity

A regular GraphQL type lives in one subgraph and is resolved entirely by that subgraph. An entity is different -- it appears in multiple subgraphs, each contributing different fields.

For a type to work as an entity, two things must be true:

1. **It has key fields** that uniquely identify each instance (like `id` or `sku`).
2. **At least one subgraph provides a lookup** -- a query field that can resolve the entity given its key fields.

In the Products subgraph, `Product` is a full type with `id`, `name`, `price`, and other fields:

```csharp
// Products/Data/Product.cs

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double Price { get; set; }
    public int Weight { get; set; }
}
```

In the Reviews subgraph, `Product` is an entity stub -- a lightweight declaration with just the key field and the new fields this subgraph contributes:

```csharp
// Reviews/Types/Product.cs

public sealed record Product([property: ID<Product>] int Id)
{
    public List<Review> GetReviews()
        => ReviewRepository.GetByProductId(Id);
}
```

The entity stub does not duplicate `name`, `price`, or `weight`. It only declares the key (`Id`) and the fields it adds (`reviews`). During composition, Fusion merges these into one `Product` type with all fields. The gateway resolves each field from the subgraph that owns it.

### Entity Stubs Are Not Code Duplication

A common concern: "Am I duplicating the Product type across subgraphs?" No. The entity stub is a declaration, not a copy. It says "I know `Product` exists, identified by `Id`, and I want to contribute fields to it." The stub has no knowledge of the other subgraph's fields, does not import the other subgraph's code, and can be deployed independently.

## Lookups

A lookup is a query field that resolves an entity by its key. The gateway uses lookups to fetch entities from the subgraph that owns them. Without a lookup, the gateway has no way to enter a subgraph and resolve an entity.

### Public Lookups

A public lookup serves two purposes: clients can call it directly as a query field, and the gateway uses it for entity resolution behind the scenes.

```csharp
// Products/Types/ProductQueries.cs

[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

The `[Lookup]` attribute tells Fusion that `GetProductByIdAsync` resolves a `Product` entity by its `id` argument. The composition engine infers from this that `id` is a key field for `Product`.

Public lookups return nullable types (`Product?`). If a client passes an ID that does not exist, the lookup returns `null`. This is correct because clients can call the lookup directly with arbitrary IDs.

### Internal Lookups

An internal lookup is hidden from the composite schema. Clients cannot call it. It exists only for the gateway to use during entity resolution.

```csharp
// Reviews/Types/ProductQueries.cs

[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product GetProductById([ID<Product>] int id)
        => new(id);
}
```

The `[Internal]` attribute hides this lookup from the composite schema. The gateway uses it when it needs to enter the Reviews subgraph to resolve `Product.reviews`, but clients never see or call it.

Internal lookups typically construct a stub object from the key without checking whether the entity actually exists (note the non-nullable return type `Product` and the simple `new(id)`). This is safe because the gateway only calls internal lookups during entity resolution, after another subgraph has already confirmed the entity exists.

### When to Use Internal vs. Public Lookups

Use a **public lookup** when:

- Your subgraph is the primary owner of the entity (the Products subgraph for `Product`, the Accounts subgraph for `User`)
- Clients should be able to query for this entity directly from your subgraph
- The lookup validates that the entity exists and returns `null` if it does not

Use an **internal lookup** when:

- Your subgraph extends an entity from another subgraph (the Reviews subgraph extending `Product`)
- You do not want clients to enter your subgraph through this lookup
- The lookup just constructs a stub -- it does not need to validate existence

Every entity that your subgraph references must have a lookup in at least one subgraph. If your subgraph extends `Product`, either your subgraph provides a lookup or another subgraph does. The gateway needs at least one lookup per entity to resolve cross-subgraph references.

### Multiple Lookups Per Entity

An entity can have multiple lookups, even in the same subgraph. This is useful when an entity can be identified by different keys.

```csharp
// Accounts/Types/UserQueries.cs

[QueryType]
public static partial class UserQueries
{
    [Lookup, NodeResolver]
    public static async Task<User?> GetUserById(
        int id,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken)
        => await userById.LoadAsync(id, cancellationToken);

    [Lookup]
    public static async Task<User?> GetUserByUsername(
        string username,
        IUserByNameDataLoader userByName,
        CancellationToken cancellationToken)
        => await userByName.LoadAsync(username, cancellationToken);
}
```

The Accounts subgraph provides two lookups for `User`: one by `id` and one by `username`. The gateway can resolve a User reference using whichever key is available. If another subgraph references a User by username (rather than by numeric ID), the gateway uses the `GetUserByUsername` lookup.

The `[NodeResolver]` attribute on `GetUserById` marks it as the Relay node resolver, enabling `node(id: "...")` queries for this entity. You can have at most one `[NodeResolver]` per entity type per subgraph.

### Argument Mapping with `@is`

When a lookup argument name does not match the entity's field name, use the `@is` directive (or its C# equivalent) to map them. For example, if your lookup uses `productId` as the argument name but the entity's key field is `id`:

```graphql
type Query {
  personById(productId: ID! @is(field: "id")): Person @lookup
}
```

The `@is` directive tells the composition engine that the `productId` argument corresponds to the `id` field on the `Person` type. When argument names match field names (which is the common case), you can omit `@is` -- the mapping is inferred automatically.

### Batch Lookups and the N+1 Problem

When the gateway resolves a list of entities (for example, fetching reviews for 10 products), it needs to call the lookup once per entity. Without batching, this creates an N+1 problem -- 1 call to get the product list, then N calls to get reviews for each product.

HotChocolate's `[DataLoader]` attribute solves this by automatically batching entity resolution. Instead of N individual calls, the gateway sends one batched request with all N keys.

```csharp
// Products/Data/ProductDataLoader.cs

internal static class ProductDataLoader
{
    [DataLoader]
    internal static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> ids,
        ProductContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
}
```

The `[DataLoader]` attribute source-generates an `IProductByIdDataLoader` interface. When your lookup uses this DataLoader, the gateway automatically batches entity resolution:

```csharp
[Lookup]
public static async Task<Product?> GetProductByIdAsync(
    int id,
    IProductByIdDataLoader productById,
    CancellationToken cancellationToken)
    => await productById.LoadAsync(id, cancellationToken);
```

Even though the lookup accepts a single `id`, the DataLoader collects all requested IDs and executes a single batch query. This turns N+1 individual database queries into one query with a `WHERE id IN (...)` clause.

Always use DataLoaders for lookup resolvers that hit a database or external service. Without them, cross-subgraph queries on lists will generate one database query per entity, which degrades performance significantly as the list grows.

## Field Ownership and `[Shareable]`

When multiple subgraphs define the same type, Fusion needs to know which subgraph owns each field. The rules are straightforward:

### Key Fields Are Automatically Shareable

Fields that serve as entity keys (referenced by lookups) are implicitly shareable. You do not need to add `[Shareable]` to key fields. Both the Products and Reviews subgraphs define `Product.id`, and this composes without conflict because `id` is a key field.

### Non-Key Fields Must Be Unique or Explicitly Shareable

By default, a non-key field must appear in exactly one subgraph. If two subgraphs define the same non-key field on the same type, composition fails with an error.

For example, if both the Accounts subgraph and the Reviews subgraph define `User.name`:

```csharp
// Accounts/Types/UserNode.cs

[ObjectType<User>]
public static partial class UserNode
{
    [Shareable]
    public static string GetName([Parent] User user)
        => user.Name!;
}
```

```csharp
// Reviews/Types/UserNode.cs

[ObjectType<User>]
internal static partial class UserNode
{
    [Shareable]
    public static string GetName([Parent] User user)
        => user.Name!;
}
```

Both subgraphs mark `GetName` with `[Shareable]`. This tells Fusion: "this field is intentionally defined in multiple subgraphs, and all definitions return the same data." The gateway can resolve the field from whichever subgraph is most convenient for a given query.

If you forget `[Shareable]` on any definition, composition fails:

```text
Error: Field "User.name" is defined in subgraphs "accounts-api" and "reviews-api"
without [Shareable]. Mark the field as [Shareable] in all subgraphs that define it,
or remove the duplicate definition.
```

### When to Use `[Shareable]`

Mark a field as `[Shareable]` when:

- Multiple subgraphs genuinely return the same data for this field
- You want the gateway to have the flexibility to resolve it from either subgraph

Do **not** use `[Shareable]` when:

- The fields return different data (use different field names instead)
- Only one subgraph should own the field (do not define it in other subgraphs)

A common use case: the Reviews subgraph stores a local copy of `User.name` for display purposes. Both the Accounts and Reviews subgraphs can resolve it, so both mark it `[Shareable]`. The gateway can resolve `User.name` from whichever subgraph it is already calling for that query, avoiding an extra cross-subgraph hop.

### The Incorrect "All Types Shareable by Default" Claim

Some earlier documentation stated that "all object types are shareable by default" in Fusion. This is incorrect. The correct behavior:

- **Key fields** (referenced by lookups) are automatically shareable -- you do not need `[Shareable]`.
- **All other fields** must be explicitly marked `[Shareable]` if defined in multiple subgraphs.
- Without `[Shareable]`, duplicate non-key fields cause a composition error.

This matches the GraphQL Composite Schemas specification: `@shareable` permits multiple subgraphs to define the same field, and without it, a field may only exist in one subgraph.

## `[EntityKey]` for Explicit Key Declaration

In most cases, you do not need to declare entity keys explicitly. The composition engine infers keys from your `[Lookup]` resolvers. If `GetProductById(int id)` is marked `[Lookup]`, Fusion infers that `id` is a key field for `Product`.

Sometimes you need to declare the key explicitly. Use `[EntityKey]` when:

- Your subgraph extends an entity but does not have its own lookup for it
- You want to be explicit about which fields form the key

```csharp
// Shipping/Types/Product.cs

[EntityKey("id")]
public sealed record Product([property: ID<Product>] int Id)
{
    public int GetDeliveryEstimate(
        string zip,
        [Require("{ weight }")] int weight)
    {
        return CalculateShipping(zip, weight);
    }
}
```

The `[EntityKey("id")]` attribute explicitly declares that `Product` is an entity identified by the `id` field. This is needed in the Shipping subgraph because it does not define its own lookup that would let the composition engine infer the key.

The argument to `[EntityKey]` is the GraphQL field name (lowercase `"id"`), not the C# property name.

## Optimization Hints: `@provides` and `@external`

In most cases, you do not need these directives. They are optimization hints that help the gateway avoid unnecessary cross-subgraph calls.

### `@provides`

The `@provides` directive (expressed in C# through `[Parent(requires: "...")]` patterns) tells the composition engine that a field resolver can supply certain subfields of its return type locally, avoiding a separate cross-subgraph call.

For example, if the Reviews subgraph stores a local copy of the user's name alongside each review:

```csharp
// Reviews/Types/ReviewNode.cs

[ObjectType<Review>]
internal static partial class ReviewNode
{
    [BindMember(nameof(Review.AuthorId))]
    public static async Task<User?> GetAuthorAsync(
        [Parent(requires: nameof(Review.AuthorId))] Review review,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken)
        => await userById.LoadAsync(review.AuthorId, cancellationToken);
}
```

When the Reviews subgraph resolves `Review.author`, it can also supply the author's `name` from its local data. The gateway knows it does not need a separate call to the Accounts subgraph just to get `User.name` -- the Reviews subgraph already has it.

### `@external`

The `@external` directive indicates that a field is defined and primarily resolved by another subgraph. It is used in conjunction with `@provides` to mark fields that this subgraph can supply in specific contexts but does not own.

In HotChocolate, you typically do not need to write `@external` explicitly. The composition engine infers external fields from entity stubs and `@provides` declarations. If you are writing schemas in SDL rather than C#, you would use `@external` on fields that another subgraph owns but that your subgraph can provide as an optimization.

## Node Pattern (Relay Global Object Identification)

The [Relay Global Object Identification specification](https://relay.dev/graphql/objectidentification.htm) defines a standard way to fetch any entity by a globally unique ID using a `node(id: "...")` query. Fusion supports this pattern through the `[NodeResolver]` attribute and `AddGlobalObjectIdentification()`.

### Enabling the Node Pattern

**In each subgraph**, register global object identification:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification()
    .AddTypes();
```

Then mark one lookup per entity type with `[NodeResolver]`:

```csharp
[QueryType]
public static partial class ProductQueries
{
    [Lookup, NodeResolver]
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

`[NodeResolver]` tells HotChocolate that this lookup is the Relay node resolver for `Product`. The `node(id: "...")` query decodes the global ID, determines the entity type, and dispatches to the correct `[NodeResolver]` lookup.

**During composition**, enable global object identification with the `--enable-global-object-identification` flag:

```bash
nitro fusion compose \
  --source-schema-file Products/schema.graphqls \
  --source-schema-file Reviews/schema.graphqls \
  --archive gateway.far \
  --enable-global-object-identification
```

This adds the `node` and `nodes` query fields to the composite schema. Without this flag, `[NodeResolver]` annotations are ignored during composition.

### When to Use It

The node pattern is useful when:

- Your clients use Relay or a client that expects global object identification
- You want a uniform way to refetch any entity by a single opaque ID
- The fusion-demo uses `[NodeResolver]` on every entity lookup as a standard practice

You can have at most one `[NodeResolver]` per entity type per subgraph. If an entity has multiple lookups (by ID, by username, etc.), only the primary one should be the `[NodeResolver]`.

## Putting It All Together

Here is a summary of the patterns for the most common entity scenarios:

**You own the entity (primary subgraph):**

- Define the full type with all fields
- Add a public `[Lookup]` resolver (with `[NodeResolver]` if using Relay)
- Use `[DataLoader]` for batch resolution

**You extend the entity (secondary subgraph):**

- Create an entity stub with just the key field and your new fields
- Add an internal `[Lookup, Internal]` resolver
- Use `[BindMember]` if replacing a foreign key with an entity reference
- Mark any duplicated non-key fields with `[Shareable]`

**You need data from another subgraph in a resolver:**

- Use `[Require(...)]` on the resolver argument to declare the dependency
- The gateway fetches the required data automatically
- Required arguments are hidden from the composite schema

**Your entity can be identified by multiple keys:**

- Add multiple `[Lookup]` resolvers (by ID, by username, by SKU, etc.)
- The gateway uses whichever key is available

## Next Steps

- **Need cross-subgraph field dependencies?** The `[Require]` attribute enables resolvers to depend on data from other subgraphs. Cross-subgraph data dependencies, including complex field mapping, will be covered in detail in future documentation.
- **Want to understand composition rules?** See [Composition](/docs/fusion/v16/composition) for how types are merged, what causes composition errors, and how to fix them.
- **Ready to go to production?** See [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization) for securing your gateway and subgraphs, or [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for setting up independent subgraph deployments.
