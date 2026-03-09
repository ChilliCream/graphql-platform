---
title: "Entities and Lookups"
---

Entities are the mechanism that makes distributed GraphQL work. They are types with stable keys that can be referenced and resolved across subgraphs. For example, the Products subgraph defines the `Product` type, and the Reviews subgraph contributes the `reviews` field to `Product`. The Accounts subgraph defines the `User` type, and other subgraphs can contribute additional fields to `User`. Without entities, each subgraph would be an isolated API. With entities, those subgraphs compose into one unified API.

This page explains entity resolution in more detail: how entities are defined and how lookups resolve them across subgraphs. If you completed the [Getting Started](/docs/fusion/v16/getting-started) tutorial, you already used these concepts. Here, you will focus on the mechanics and patterns behind them.

## What Makes a Type an Entity

A type is not an entity because it appears in multiple subgraphs. It is an entity because it has stable key-based identity. An entity is a type with one or more key fields that uniquely identify an instance across multiple subgraphs. Those key fields form the contract between subgraphs: one subgraph can return an entity reference by key, and another subgraph can use that key to resolve additional fields for the same instance.

In practice, two requirements matter:

1. **Entity identity:** one or more key fields uniquely identify each instance, like `id` or `sku`.
2. **Entity resolution:** at least one lookup is available so the gateway can resolve references by key.

```graphql
# Products subgraph
type Product {
  id: ID!
  name: String!
}

type Query {
  productById(id: ID!): Product @lookup
}
```

Another subgraph can reuse the same key and contribute fields.

```graphql
# Reviews subgraph
type Product {
  id: ID!
  reviews: [Review!]!
}

type Query {
  productById(id: ID!): Product @lookup @internal
}
```

In these examples, `id` is the key and `@lookup` defines how `Product` is resolved by that key. The Reviews lookup is internal, so clients cannot call it directly, but the gateway can use it to enter the Reviews subgraph and resolve `reviews`.

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

## Next Steps

- **Need field ownership rules?** See [Composition](/docs/fusion/v16/composition) for how field ownership, `@shareable`, and composition validation work.
- **Need argument mapping and cross-subgraph dependencies?** The `@is` and `@require` directives are covered in dedicated pages.
- **Need runtime performance guidance?** See Hot Chocolate docs for DataLoader and batching patterns used inside lookup resolvers.
- **Ready to go to production?** See [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization) for securing your gateway and subgraphs, or [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for setting up independent subgraph deployments.
