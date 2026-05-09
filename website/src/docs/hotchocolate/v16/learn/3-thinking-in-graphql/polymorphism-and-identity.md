---
title: "Polymorphism and identity"
description: "How and when to use stable IDs, global object identification, interfaces, unions, and fragments in Hot Chocolate v16 schemas."
---

When building a GraphQL API, you will often encounter situations where the same logical object appears in multiple places. For example, a product might show up in a search result, a recommendation list, and a product detail page. Similarly, a customer could be accessed directly or through an order. To make these patterns work for clients, your schema needs to express both identity (how to recognize and refetch the same object) and polymorphism (how to handle fields that can return more than one type).

This operation shows the problem in one response. The same customer can arrive through a direct lookup and through an order relationship, and the client needs a stable way to recognize it as the same logical object.

```graphql
query CustomerFromTwoPaths($customerId: ID!) {
  customerById(id: $customerId) {
    id
    displayName
  }
  orderById(id: "order-123") {
    customer {
      id
      displayName
    }
  }
}
```

This page guides you through the key decisions for identity and polymorphism in Hot Chocolate v16. It focuses on the contract you offer to clients, not the implementation details. For syntax and configuration, see the Hot Chocolate docs for [object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces/), [unions](/docs/hotchocolate/v16/building-a-schema/unions/), and [Relay global object identification](/docs/hotchocolate/v16/building-a-schema/relay/).

# Why stable identity matters

A stable object type should have an `id` field if clients need to:
- Navigate to it from more than one place
- Compare it with other results
- Store it in a cache
- Link to it or refetch it later

For example:

```graphql
type Product {
  id: ID!
  name: String!
  sku: String!
}
```

The `id` field is the API's promise that this value refers to the same logical `Product` across requests and over time. The value should not reveal database details, sequences, or storage layout. Use the GraphQL [`ID` scalar](https://spec.graphql.org/October2021/#sec-ID) for identifiers that represent identity. The `ID` scalar is serialized as a string, and clients should treat it as opaque unless your schema documentation guarantees a format.

Not every object type needs an ID. Value objects, such as `Money` or `Address`, are typically read as part of another object and are not refetchable on their own:

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

Adding IDs to value objects can mislead clients into thinking these values have an independent lifecycle. Before adding an `id`, ask: do clients need to recognize or refetch this object independently?

# Choosing local, opaque, or global IDs

Your ID strategy is a contract with clients. Choose the smallest contract that meets their needs and allows for future server changes. Here is a summary to help you decide:

| Client need | ID shape | Reason | Read next |
| --- | --- | --- | --- |
| Detail page refetches `Product` by ID | Stable opaque `ID` on `Product` | Lets the client store and use an API identifier without exposing persistence details | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Normalized client cache stores many object types | Consider global IDs and `Node` | A single identity space helps caches and refetch workflows across types | [Relay](/docs/hotchocolate/v16/building-a-schema/relay/) |
| Search result links to products, articles, and users | Global ID or type-specific opaque IDs | The server may need type and routing context to resolve the link | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Report row has no independent lifecycle | No ID | The row is a projection, not a durable object | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |
| Child detail exists only under a parent | Local ID or no ID | The parent is the real identity boundary | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |

- **Local IDs** identify an object within a type, field, or parent context.
- **Opaque IDs** hide the server's internal representation and can encode routing context (type, tenant, shard, etc) without exposing it to clients.
- **Global IDs** identify objects across types in a single identity space, which is useful for links, notifications, search hits, or caches that span multiple types.

Global identification is a design choice, not a badge of maturity. It can be more contract than you need for small internal schemas, admin screens, value-like objects, projections, or relationship details.

In Hot Chocolate v16, configure ID behavior intentionally. By default, a C# property named `Id` may infer as its CLR type (such as `Int!`) in the GraphQL schema unless you explicitly configure it as GraphQL `ID` or opt into global ID behavior. Global identifier serialization and `Node` refetching are enabled through the Relay/global object identification features you choose to use.

# When to use global object identification

Global object identification gives clients a single entry point for refetching any object by its ID, regardless of type. This is especially useful when clients store IDs and later need to retrieve the object without knowing which root field to call.

```graphql
interface Node {
  id: ID!
}

type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

A global ID combines object identity with enough type information for the server to resolve it through `node(id:)` or `nodes(ids:)`. Hot Chocolate provides this through its [Relay support](/docs/hotchocolate/v16/building-a-schema/relay/).

Example query:

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

This pattern is valuable for normalized caches, which often use `__typename` plus a stable `id`, or a global ID, to merge data from different queries. See [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) for more on fragments and cache strategies.

Remember, a global ID is not a permission grant. The server still controls authorization, tenancy, and not-found behavior. The `node` resolver decides whether the current request can access the object and how to respond if it no longer exists. Connect these decisions to your [errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) strategy.

If you adopt global identification, provide both `node(id:)` and `nodes(ids:)` so clients can refetch one or many objects in a single operation.

# Interfaces: sharing contracts across types

Use an interface when multiple object types promise the same fields and clients need to select those fields through the abstract type. For example:

```graphql
interface Node {
  id: ID!
}

type Product implements Node {
  id: ID!
  name: String!
}

type User implements Node {
  id: ID!
  displayName: String!
}
```

Interfaces are public contracts. Every implementation must satisfy the interface. Clients can select `id` on `Node` before knowing whether the object is a `Product` or a `User`.

Good interface names describe domain behavior or capability:

| Interface | When to use |
| --- | --- |
| `Node` | For globally identifiable and refetchable objects |
| `Named` | For objects with a public display name |
| `Commentable` | For objects that support comments |
| `SearchResult` | For search members sharing fields like `title` and `url` |

Before publishing an interface, check that every implementation can safely substitute for the interface in client operations. If a field only applies to some implementations, it may not belong on the interface.

Do not create interfaces only because C# classes share a base type or database tables share columns. GraphQL interfaces exist for client selections. Avoid vague interfaces like `Entity` with only `id` and no meaningful client behavior.

Adding a new implementation to an interface can affect clients that assume they know all possible types. Treat these changes as schema evolution events and review them with [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Unions: representing heterogeneous results

Use a union when a field can return several object types that do not share meaningful fields beyond the `__typename` meta-field. For example:

```graphql
type Product {
  id: ID!
  name: String!
}

type Article {
  id: ID!
  title: String!
}

type User {
  id: ID!
  displayName: String!
}

union SearchResult = Product | Article | User
```

Clients use `__typename` and inline fragments to select fields from each concrete type:

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

Unions are common for search results, activity feeds, recommendations, and workflow outcomes. The union signals that each member is a valid result, but clients must branch by concrete type.

If clients always need the same fields from every member, consider an interface instead. If a field uses a single object type with many nullable fields to represent different cases, a union or interface can make invalid combinations unrepresentable.

For example, this shape allows multiple fields to be present or absent:

```graphql
type CheckoutOutcome {
  order: Order
  paymentRequired: PaymentRequired
  validationError: ValidationError
}
```

A union can express distinct valid outcomes:

```graphql
union CheckoutOutcome =
    CheckoutSuccess
  | PaymentRequired
  | ValidationError
```

For details on Hot Chocolate union registration and type resolution, see [Unions](/docs/hotchocolate/v16/building-a-schema/unions/).

# Avoid fake polymorphism

Fake polymorphism occurs when an abstraction does not help clients select fields or complete workflows. For example:

```graphql
type Entity {
  id: ID!
  kind: String!
  payload: String!
}

type Query {
  entityById(id: ID!): Entity
}
```

This shape forces clients to interpret `kind` and `payload`, guessing which fields are safe to use. Instead, use specific object types and unions where they add value:

```graphql
type Query {
  productById(id: ID!): Product
  userById(id: ID!): User
  search(term: String!): [SearchResult!]!
}

type Product implements Node {
  id: ID!
  name: String!
}

type User implements Node {
  id: ID!
  displayName: String!
}

type Article {
  id: ID!
  title: String!
}

union SearchResult = Product | User | Article
```

Avoid these patterns unless a client-facing contract proves their value:
- Database inheritance copied into GraphQL inheritance
- One generic `Entity` abstraction for every row
- Broad unions containing unrelated types
- Marker interfaces with no fields or client behavior
- Type names or fields that mirror server base classes instead of domain language

For field placement, focus on what contract helps clients complete a task, not what shape exists in storage. See [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Designing for clients and tooling

Abstract types affect client code and generated types. Review the generated and runtime shapes before publishing. For example:

```graphql
query SearchCards($term: String!) {
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

Clients often need `__typename` for abstract selections, UI branching, normalized stores, and cache keys. Fragments document what each client needs from each possible type.

Generated clients may model interfaces and unions as base interfaces, subclasses, discriminated unions, sealed hierarchies, variant records, or fragment result types. The exact shape depends on the client tooling. For .NET clients, see [Strawberry Shake](/docs/strawberryshake/v16). For broader client responsibilities, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

Adding a possible type to an interface or union can be a breaking change for clients that use exhaustive switches, generated discriminated unions, fragment masks, or cache policies. The schema may remain valid, but client code may need review.

For every abstract type change, ask: if we add a possible type, which clients must update fragments, generated code, exhaustive switches, tests, or cache rules?

# Reviewing abstract type changes with Nitro and client tooling

Use Nitro to inspect the published abstract contract and validate who is affected by a possible-type change:

1. Inspect the interface or union in Nitro's [Schema Reference](/docs/nitro/documents/schema-reference/) or [Schema Definition](/docs/nitro/documents/schema-definition/) view.
2. Confirm each implementing type or union member belongs to the public contract.
3. Run a focused operation in Nitro [Operations](/docs/nitro/documents/operations/) that includes `__typename`, common interface fields, and inline fragments for each expected concrete type.
4. Inspect the [Response](/docs/nitro/documents/response/) to confirm runtime `__typename` values and fragment results match the schema contract.
5. Use the [schema registry](/docs/nitro/apis/schema-registry/) to detect dangerous abstract-type changes before rollout.
6. Use the [client registry](/docs/nitro/apis/client-registry/) when available to identify clients and persisted documents that select the abstract type.
7. Regenerate or review clients that depend on exhaustive abstract selections, fragment masks, discriminated unions, or normalized cache policies.

Nitro does not decide whether a field should be an interface or union. The schema author chooses based on client needs, domain language, and evolution risk. Nitro's registry and operation workflow provide a safety check before clients receive the change.

# Identity and polymorphism review checklist

Use this checklist during schema review before implementing descriptors, attributes, resolvers, or client code:

- Does the object have an independent lifecycle that clients need to recognize or refetch?
- Is the `id` stable, opaque, and safe to expose?
- Is the ID local to a type or parent context, or should it be global across object types?
- If global identification is adopted, are both `node(id:)` and `nodes(ids:)` part of the contract?
- Is this object truly refetchable, or is it a value object, child-only object, projection, or relationship detail?
- Does the interface promise shared fields clients can select?
- Can every implementation safely substitute for the interface in client operations?
- Does the union represent heterogeneous results without a shared contract?
- Would an interface or union remove invalid nullable-field combinations from the schema?
- Are `__typename`, fragments, generated types, and normalized cache keys accounted for?
- Would adding a new possible type be treated as a client-review event?
- Have you inspected possible types and run an abstract-selection operation in Nitro before publishing?
- Have schema registry or client registry checks identified affected clients or persisted documents?
- Is any abstraction copied from database inheritance or a server base class rather than public API needs?

For each globally identifiable object, interface, or union, write a short design note naming the client task, the identity strategy, the possible concrete types, and which clients must review future possible-type changes.

# Next steps

- Implement your schema shape with [object types](/docs/hotchocolate/v16/building-a-schema/object-types/), [interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces/), [unions](/docs/hotchocolate/v16/building-a-schema/unions/), and [Relay](/docs/hotchocolate/v16/building-a-schema/relay/).
- Review operation shape, fragments, and cache behavior in [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) and [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).
- Check list identity and connection behavior in [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/).
- Plan rollout and possible-type changes with [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).
- Use the GraphQL specification for the underlying rules on [interfaces](https://spec.graphql.org/October2021/#sec-Interfaces), [unions](https://spec.graphql.org/October2021/#sec-Unions), [fragments](https://spec.graphql.org/October2021/#sec-Fragments), and the [`ID` scalar](https://spec.graphql.org/October2021/#sec-ID).
