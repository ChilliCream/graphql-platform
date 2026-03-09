---
title: "Entities and Lookups"
---

Entities are the mechanism that makes distributed GraphQL work. They are types with stable keys that can be referenced and resolved across subgraphs. For example, the Products subgraph defines the `Product` type, and the Reviews subgraph contributes the `reviews` field to `Product`. The Accounts subgraph defines the `User` type, and other subgraphs can contribute additional fields to `User`. Without entities, each subgraph would be an isolated API. With entities, those subgraphs compose into one unified API.

This page explains entity resolution in more detail: how entities are defined, how lookups resolve them across subgraphs, and how field ownership is enforced. If you completed the [Getting Started](/docs/fusion/v16/getting-started) tutorial, you already used these concepts. Here, you will focus on the mechanics and patterns behind them.

## What Makes a Type an Entity

A regular GraphQL type lives in one subgraph and is resolved entirely by that subgraph. An entity is identified by a stable key that can be referenced across subgraphs.

For practical use in Fusion, separate these concerns:

1. **Entity identity:** one or more key fields uniquely identify each instance, like `id` or `sku`.
2. **Entity resolution:** at least one lookup exists in the composed system so the gateway can resolve references by key.

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

In the Reviews subgraph, `Product` is an entity stub, a lightweight declaration with just the key field and the new fields this subgraph contributes:

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

Lookups should return nullable entity types (`Product?`). This allows unresolved keys to return `null` and helps avoid cascading failures when one or more subgraphs cannot provide additional fields for an entity.

### Internal Lookups

An internal lookup is hidden from the composite schema. Clients cannot call it. It exists only for the gateway to use during entity resolution.

```csharp
// Reviews/Types/ProductQueries.cs

[QueryType]
public static partial class ProductQueries
{
    [Lookup, Internal]
    public static Product? GetProductById([ID<Product>] int id)
        => new(id);
}
```

The `[Internal]` attribute hides this lookup from the composite schema. The gateway uses it when it needs to enter the Reviews subgraph to resolve `Product.reviews`, but clients never see or call it.

Lookups should return nullable entity types (`Product?`) so unresolved keys can return `null` and avoid cascading failures across subgraphs. Internal lookups often still construct a stub from the key without checking existence, because the gateway calls them during entity resolution with already-known keys.

### When to Use Internal vs. Public Lookups

Use a **public lookup** when:

- Your subgraph is the primary owner of the entity (the Products subgraph for `Product`, the Accounts subgraph for `User`)
- Clients should be able to query for this entity directly from your subgraph
- The lookup validates that the entity exists and returns `null` if it does not

Use an **internal lookup** when:

- Your subgraph extends an entity from another subgraph (the Reviews subgraph extending `Product`)
- You do not want clients to enter your subgraph through this lookup
- The lookup just constructs a stub. It does not need to validate existence.

For cross-subgraph resolution to work, the composed API needs at least one lookup per referenced entity. If your subgraph extends `Product`, either your subgraph defines that lookup or another subgraph does.

### Multiple Lookups Per Entity

An entity can have multiple lookups, even in the same subgraph. This is useful when an entity can be identified by different keys.

```csharp
// Accounts/Types/UserQueries.cs

[QueryType]
public static partial class UserQueries
{
    [Lookup]
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

The Accounts subgraph defines two lookups for `User`: one by `id` and one by `username`. The gateway can resolve a User reference using whichever key is available. If another subgraph references a User by username, the gateway uses `GetUserByUsername`.

### Argument Mapping with `@is`

When a lookup argument name does not match the entity's field name, use the `@is` directive (or its C# equivalent) to map them. For example, if your lookup uses `productId` as the argument name but the entity's key field is `id`:

```graphql
type Query {
  personById(productId: ID! @is(field: "id")): Person @lookup
}
```

The `@is` directive tells the composition engine that the `productId` argument corresponds to the `id` field on the `Person` type. When argument names match field names, which is the common case, you can omit `@is` because the mapping is inferred automatically.

### Batch Lookups and the N+1 Problem

When the gateway resolves a list of entities (for example, fetching reviews for 10 products), it needs to call the lookup once per entity. Without batching, this creates an N+1 problem: one call to get the product list, then N calls to get reviews for each product.

Hot Chocolate's `[DataLoader]` attribute solves this by automatically batching entity resolution.

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

In short: key fields used for entity identity are implicitly shareable, while duplicate non-key fields require `[Shareable]` on every definition.

## `[EntityKey]` for Explicit Key Declaration

In most cases, you do not need to declare entity keys explicitly. The composition engine infers keys from your `[Lookup]` resolvers. If `GetProductById(int id)` is marked `[Lookup]`, Fusion infers that `id` is a key field for `Product`.

Sometimes you need to declare the key explicitly. Use `[EntityKey]` when:

- Your subgraph extends an entity but does not have its own lookup for it
- You want to be explicit about which fields form the key

```csharp
// Shipping/Types/Product.cs

[EntityKey("id")]
public sealed record Product([property: ID<Product>] int Id);
```

The `[EntityKey("id")]` attribute explicitly declares that `Product` is an entity identified by the `id` field. This is needed in the Shipping subgraph because it does not define its own lookup that would let the composition engine infer the key.

The argument to `[EntityKey]` is the GraphQL field name (lowercase `"id"`), not the C# property name.

## Putting It All Together

Here is a summary of the patterns for the most common entity scenarios:

**You own the entity (primary subgraph):**

- Define the full type with all fields
- Add a public `[Lookup]` resolver
- Use `[DataLoader]` for batch resolution

**You extend the entity (secondary subgraph):**

- Create an entity stub with just the key field and your new fields
- Add an internal `[Lookup, Internal]` resolver
- Use `[BindMember]` if replacing a foreign key with an entity reference
- Mark any duplicated non-key fields with `[Shareable]`

**Your entity can be identified by multiple keys:**

- Add multiple `[Lookup]` resolvers (by ID, by username, by SKU, etc.)
- The gateway uses whichever key is available

## Next Steps

- **Need cross-subgraph field dependencies?** The `[Require]` attribute enables resolvers to depend on data from other subgraphs. Cross-subgraph data dependencies, including complex field mapping, will be covered in detail in future documentation.
- **Want to understand composition rules?** See [Composition](/docs/fusion/v16/composition) for how types are merged, what causes composition errors, and how to fix them.
- **Ready to go to production?** See [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization) for securing your gateway and subgraphs, or [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for setting up independent subgraph deployments.
