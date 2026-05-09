---
title: "Polymorphism and identity"
description: "Design stable IDs, global object identification, interfaces, and unions around real client needs in Hot Chocolate v16."
---

Clients often meet the same logical object through different paths. A customer can appear at `viewer.orders.nodes[0].customer` and through `customer(id:)`. If your schema does not define identity and abstract type contracts clearly, clients cannot refetch confidently, cache safely, or evolve with you.

This page helps you decide:

- when an object needs a stable ID
- when global object identification helps
- when interfaces fit better than unions
- how abstract types affect generated clients and rollout

For implementation syntax, use the Hot Chocolate v16 reference for [object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces/), [unions](/docs/hotchocolate/v16/building-a-schema/unions/), and [Relay global object identification](/docs/hotchocolate/v16/building-a-schema/relay/).

# Give stable objects stable identity

Start from the client task. If clients need to recognize, compare, cache, link, or refetch an object independently, give that object a stable API identity.

```graphql
type Product {
  id: ID!
  name: String!
}
```

`id` is a contract. It means this value refers to the same logical `Product` across requests and over time.

Treat IDs as API identity, not database disclosure. Clients should not depend on table names, numeric sequences, or storage layout. The GraphQL [`ID` scalar](https://spec.graphql.org/October2021/#sec-ID) is designed for this purpose.

Not every type needs identity. Some values are part of another object and have no independent lifecycle.

```graphql
type Money {
  amount: Decimal!
  currencyCode: String!
}

type Address {
  line1: String!
  city: String!
  countryCode: String!
}
```

Before adding an `id`, ask: can clients refetch this object as its own thing, or is it value data carried by another object?

# Choose local, opaque, or global IDs deliberately

Choose the smallest identity contract that satisfies client behavior.

| Client need | Recommended ID shape | Why |
| --- | --- | --- |
| Product detail view refetches by ID | Stable opaque ID on `Product` | Supports linking, caching, and refetch without exposing persistence details |
| Cache stores many types in one normalized store | Consider global IDs with `Node` | Provides one identity space across object types |
| Search rows have no independent lifecycle | No ID or parent-scoped ID | Avoids artificial identity contracts |
| Child object exists only under parent | Local ID or no ID | Parent is the identity boundary |

- **Local ID**: unique inside a type or parent context.
- **Opaque ID**: server controlled representation that can carry routing context without leaking storage semantics.
- **Global ID**: unique across participating object types.

Global IDs are useful in many systems. They are not mandatory for every schema. Small internal graphs and value-like types often work better with local identity, or with no identity at all.

# Use global identification when clients need one refetch contract

Global object identification helps when clients store an ID now and refetch later through a single entry point.

```graphql
query Refetch($id: ID!) {
  node(id: $id) {
    __typename
    id
    ... on Product {
      name
    }
    ... on User {
      displayName
    }
  }
}
```

If you adopt this contract, publish both `node(id:)` and `nodes(ids:)` so clients can refetch one or many objects consistently. Hot Chocolate supports this through [Relay global object identification](/docs/hotchocolate/v16/building-a-schema/relay/).

This pattern is often valuable for links, notifications, activity items, and cross-domain references where the concrete type may vary.

Global IDs do not bypass authorization or tenancy rules. The server still decides access, not-found behavior, and deleted object handling.

# Use interfaces for shared contracts

Use an interface when multiple concrete types share fields that clients should select through the abstract type.

```graphql
interface Node {
  id: ID!
}

type Product implements Node {
  id: ID!
  name: String!
}
```

An interface is a public promise. Every implementation must satisfy that shared field contract.

Use interface names that describe client-visible capability, such as `Node`, `Named`, or `Commentable`. Avoid vague abstractions like `Entity` when they do not represent a useful client contract.

Run a substitutability check: if a client asks for the interface fields, is every implementation valid in that position?

# Use unions for heterogeneous results without a shared contract

Use a union when a field can return different object types that do not share meaningful common fields.

```graphql
union SearchResult = Product | Article | User
```

Clients branch with `__typename` and inline fragments:

```graphql
query Search($term: String!) {
  search(term: $term) {
    __typename
    ... on Product {
      id
      name
    }
    ... on Article {
      id
      title
    }
    ... on User {
      id
      displayName
    }
  }
}
```

If clients always need the same fields from all members, use an interface instead.

Unions are common for search results, feeds, recommendations, and operation outcomes.

# Avoid fake polymorphism

Do not mirror server inheritance or database categories unless that abstraction improves client behavior.

Watch for these warning signs:

- a giant `Entity` type with generic payload fields
- unions that group unrelated types
- interfaces with no meaningful shared fields

Model public capabilities, not persistence structure. Favor domain terms and client tasks.

For field placement decisions, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Design abstract types for clients and generated tooling

Abstract types shape client code generation, fragment design, and cache behavior.

- Include `__typename` in abstract selections.
- Use fragments to document what each client needs from each concrete type.
- Plan for possible-type changes as a client review event.

Generated clients may model interfaces and unions as base types, discriminated unions, or variant records depending on tooling. A new possible type can require updates to exhaustive switches, fragment coverage, and cache rules.

Use [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) as companion guides when you publish abstract type changes.

# Review abstract type changes with Nitro and client tooling

Use Nitro to inspect the published contract and validate impact before rollout:

1. Inspect interfaces, unions, and possible types in [Schema Reference](/docs/nitro/documents/schema-reference/) or [Schema Definition](/docs/nitro/documents/schema-definition/).
2. Run an operation in [Operations](/docs/nitro/documents/operations/) with `__typename` and inline fragments.
3. Verify runtime shape in [Response](/docs/nitro/documents/response/).
4. Check [schema registry](/docs/nitro/apis/schema-registry/) impact for dangerous abstract type changes.
5. Use [client registry](/docs/nitro/apis/client-registry/) when available to identify affected persisted operations and clients.
6. Regenerate or review client code where exhaustive handling is expected.

Nitro supports contract review. The schema design decision still comes from your client workflows.

# Identity and polymorphism review checklist

- Does this object have an independent lifecycle that clients need to recognize or refetch?
- Is its ID stable, opaque, and safe to expose?
- Is local identity enough, or is global identity required?
- If global identity is required, do you provide both `node(id:)` and `nodes(ids:)`?
- Is this object truly refetchable, or value-like and parent-bound?
- Does each interface expose fields clients can rely on before type-specific fragments?
- Can every implementation safely substitute for the interface contract?
- Does each union represent genuinely heterogeneous results with no strong shared field contract?
- Are `__typename`, fragments, generated types, and cache keys accounted for?
- Will adding a possible type trigger client review and rollout planning?
- Have you inspected the abstract type and runtime shape in Nitro before publishing?
- Are schema registry and client registry checks part of your release review when available?
- Are abstractions driven by API needs rather than storage inheritance?

For each globally identifiable type, interface, or union, keep a short design note: client task, identity strategy, possible types, and client groups to review on future changes.
