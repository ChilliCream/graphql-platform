---
title: "Field Ownership"
---

Field ownership defines which subgraph is responsible for each field in the composite schema. Clear ownership boundaries keep composition predictable and prevent semantic drift across teams.

This chapter explains Fusion's ownership model, when to use `@shareable`, and how `@external` and `@provides` fit into ownership contracts.

## Default Ownership Rules

For non-key fields, Fusion expects a single owner.

- If one subgraph defines `Product.price`, that subgraph owns `Product.price`.
- If multiple subgraphs define the same non-key field without an explicit sharing contract, composition fails.

Key fields are special. Fields used for entity identity and lookup mapping can appear in multiple subgraphs as part of entity resolution.

## Shared Fields

Use `@shareable` when the same field is intentionally defined in multiple subgraphs and has the same meaning and value semantics in each definition.

`@shareable` is a contract, not just a conflict suppressor.

- It signals intentional overlap.
- It requires team alignment on field semantics.
- It allows the gateway to resolve the field from different subgraphs depending on the operation plan.

Let's assume we have two subgraphs, both define `User.name` as `@shareable`, so composition succeeds and the gateway can resolve the field from either source:

**GraphQL schema**

```graphql
# Accounts subgraph
type User {
  id: ID!
  name: String! @shareable
}

# Reviews subgraph
type User {
  id: ID!
  name: String! @shareable
  reviews: [Review!]!
}
```

**C# declaration**

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [Shareable]
    public static string GetName([Parent] User user)
        => user.Name!;
}
```

### When Not to Share

Do not use `@shareable` when fields are only superficially similar.

- Different semantics: `displayName` vs internal account name.
- Different staleness guarantees.
- Different normalization rules.

If the meaning differs, use different field names and keep a single owner per field.

## Contextual Field Availability

`@provides` declares that a field returning an entity can also provide selected subfields of that entity in that specific path. This is a contextual optimization, not a transfer of global ownership.

`@external` marks these field as owned by another subgraph.

**GraphQL schema**

```graphql
# Reviews subgraph
type Review {
  id: ID!
  author: User @provides(fields: "username")
}

type User {
  id: ID!
  username: String! @external
}
```

In this example:

- Accounts still owns `User.username`.
- Reviews can provide `username` only when resolving `Review.author`.

For detailed `@provides` patterns and FieldSelectionMap syntax, see [Data Requirements and Mapping](/docs/fusion/v16/data-requirements-and-mapping).

## Common Ownership Failures

These are the most frequent ownership mistakes that cause composition to fail or produce unexpected behavior.

### Duplicate Non-Key Field Without Sharing

If two subgraphs define the same non-key field without `@shareable`, composition fails.

Typical fix:

1. Remove the duplicate field from one subgraph, or
2. Mark all definitions with `@shareable` and align semantics.

### Misusing Provides as Ownership

`@provides` is a load optimization for partial availability. Use it when only some paths in a subgraph can supply a field. If your subgraph can always provide a field, use `@shareable` instead. Marking it `@external` and adding `@provides` to every path that returns the entity adds complexity for no benefit.

## Ownership Checklist

Before composition, verify:

1. Every non-key field has one clear owner.
2. Every shared field is explicitly marked `@shareable` in all defining subgraphs.
3. Shared fields have aligned semantics across teams.
4. `@external` is used only for fields owned elsewhere.
5. `@provides` is used for contextual availability, not to hide ownership ambiguity.

## Next Steps

- **Need identity and lookup routing?** See [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).
- **Need dependency and mapping syntax?** See [Data Requirements and Mapping](/docs/fusion/v16/data-requirements-and-mapping).
- **Need merge and validation rules?** See [Composition](/docs/fusion/v16/composition).
