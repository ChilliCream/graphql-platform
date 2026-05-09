---
title: "Schema design principles"
description: "Design a Hot Chocolate schema as a typed product contract that clients can understand, generate against, and evolve safely."
---

GraphQL enables clients to request exactly the data they need, even when those needs differ across web, mobile, and reporting applications. Instead of building separate endpoints for each variant, you can offer a single, flexible graph. However, this flexibility only works when the schema is treated as a contract, not as a reflection of backend implementation. Hot Chocolate can map C# types and services into GraphQL, but the public schema should be shaped by product needs, not by convenience.

# Design the schema as a product contract

A GraphQL schema defines what clients can ask for: [types](https://spec.graphql.org/October2021/#sec-Types), fields, arguments, input objects, enum values, descriptions, and nullability. These elements form the contract that clients use to build operations.

Every public field in the schema is a promise to clients. Reviewers should understand:

| Promise | What reviewers should understand |
| --- | --- |
| Meaning | The business concept the field represents |
| Shape | The type, field, argument, list, and nullability contract clients can rely on |
| Cost | The work the field may trigger when selected |
| Authorization | Which clients can see the field and whether child fields have separate rules |
| Evolution | How the field can be added, deprecated, or replaced in the future |

For example, REST endpoint design might lead to routes like `/users/{id}/address`, `/mobile/users/{id}/address`, or `/checkout/users/{id}/shipping-address`. In GraphQL, these are often modeled as a relationship:

```graphql
type User {
  id: ID!
  address: Address
}

type Address {
  street: String!
  city: String!
  countryCode: String!
}
```

Clients decide whether to select `address`, but the server team defines what `User.address` means. This field becomes a schema coordinate that clients reference, document, generate code against, and monitor.

The level of review required depends on the audience. Public graphs need strong governance, since any client may depend on any field. First-party graphs can coordinate changes with known clients, but the schema still serves as a shared language between teams. For more on boundary models, see the [public API guide](/docs/hotchocolate/v16/guides/public-api/) and [first-party API guide](/docs/hotchocolate/v16/guides/private-api/).

# Start from client tasks and domain language

Begin with the tasks clients need to accomplish. Translate repeated client questions into domain concepts, relationships, entry points, and commands.

GraphQL is client-driven in terms of selection, but not client-owned in schema design. The first client provides evidence for the minimum contract, but always consider whether names and shapes will still make sense as new clients are added.

| Client task | Contract question | Possible schema shape |
| --- | --- | --- |
| Show a product detail page | Which product facts and relationships are part of the product domain? | `productById(id: ID!): Product` and `Product.reviews` |
| Show a customer's order history | Is order history a relationship on the customer or an entry point with search rules? | `Customer.orders(first: Int!, after: String): OrderConnection` |
| Evaluate sales for a date range | Is this a report rather than an entity lookup? | `salesReport(range: DateRangeInput!): SalesReport!` |
| Cancel an order | Which command changes state and what result does the client need after the change? | `cancelOrder(input: CancelOrderInput!): CancelOrderPayload!` |

Favor business domain names over database tables, service methods, transport details, or UI component names. For example, a screen called "Account Overview" might map to schema fields like `customer`, `orders`, `billingAddress`, or `membershipStatus`.

Before adding a field, try saying the client operation out loud. If it sounds like product language, you are likely on the right track. If it sounds like a controller action, storage column, or UI component, reconsider the design. For more on field placement, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Expose fewer fields on purpose

A well-designed schema is minimal but complete. Expose only the fields needed for clients to accomplish their tasks.

Do not add fields because they exist in a C# type, database table, or service response. Hot Chocolate can infer fields from .NET members, but you can rename, ignore, or configure fields to control the contract clients see. See the [object types reference](/docs/hotchocolate/v16/building-a-schema/object-types/) for details.

Before making a field public, consider:

| Review question | Why it matters |
| --- | --- |
| Which client task uses this field? | Unused fields still require documentation, authorization, tests, and evolution planning. |
| What business decision does it support? | Fields without domain meaning are difficult for clients to discover and use correctly. |
| Can you authorize it consistently? | Fields that mix public and restricted data may need a different shape. |
| Is the expected cost acceptable? | Fields that trigger expensive operations need guardrails or a different access pattern. |
| Would you publish it in client-facing docs? | If not, it may belong in a resolver, service, DTO, or private graph. |

Favor stable relationships and entry points over variants for every current view. Leave out speculative fields until a real client need arises. Fewer fields improve discoverability and reduce the burden of schema evolution, especially for APIs with unknown consumers.

If your implementation contains more data than the contract should expose, keep that mapping inside resolvers and services. For more on this boundary, see [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/).

# Name fields and types consistently

Consistent naming makes the schema easier to navigate and shapes generated client types, normalized store records, operation reviews, documentation, and future deprecation work.

Use a single term for each concept across fields, arguments, input objects, payload objects, enum values, descriptions, examples, and client-facing documentation. Avoid abbreviations unless they are the established domain term.

Before:

```graphql
type Query {
  GetUserOrders(userId: ID!): [Order!]!
  customerOrdersForPage(customerId: ID!, pageName: String!): [Order!]!
}

type Order {
  id: ID!
}
```

After:

```graphql
type Query {
  customerById(id: ID!): Customer
}

type Customer {
  orders(first: Int!, after: String): OrderConnection!
}

type OrderConnection {
  nodes: [Order!]!
}

type Order {
  id: ID!
}
```

Mutation names should read as commands that cause side effects. Use clear `verbEntity` patterns like `cancelOrder` or `updateProductPrice`. Stick to consistent verbs for similar actions, such as `create`, `update`, `delete`, `cancel`, or `submit`, or use a domain-specific command.

Before:

```graphql
type Mutation {
  DoCancel(id: ID!): Boolean!
}
```

After:

```graphql
type Mutation {
  cancelOrder(input: CancelOrderInput!): CancelOrderPayload!
}

input CancelOrderInput {
  orderId: ID!
  reason: OrderCancellationReason!
}

type CancelOrderPayload {
  order: Order
  errors: [CancelOrderError!]!
}

type Order {
  id: ID!
}

type CancelOrderError {
  message: String!
}

enum OrderCancellationReason {
  CUSTOMER_REQUEST
}
```

Choose specific names that leave room for future expansion. For example, `price` might be clear on `Product`, but a sales report could require `grossSales`, `netSales`, `discountAmount`, and `taxAmount`. Specific names help prevent broad fields from splitting into incompatible concepts later.

Hot Chocolate applies GraphQL naming conventions when mapping C# members to schema names. Method names are converted to camel case, and the framework can handle common prefixes or suffixes. See [Building a schema](/docs/hotchocolate/v16/building-a-schema/) for mapping rules, and review [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) before renaming fields that clients already use.

# Write descriptions that explain meaning

Descriptions are part of the schema contract. They appear in schema explorers, editor tools, generated documentation, and introspection results. The [GraphQL specification defines descriptions](https://spec.graphql.org/October2021/#sec-Descriptions) as schema documentation, and Hot Chocolate lets you add them through attributes, XML documentation comments, or code-first descriptors.

Good descriptions clarify meaning that clients cannot infer from the name alone:

```graphql
type Money {
  amount: Decimal!
  currency: String!
}

type Product {
  "Current selling price in the product catalog currency."
  price: Money!

  "Reviews approved for public display, newest first."
  reviews(first: Int!, after: String): ReviewConnection!
}

type Order {
  "Current fulfillment state. CANCELED orders do not ship."
  status: OrderStatus!
}

type ReviewConnection {
  nodes: [Review!]!
}

type Review {
  rating: Int!
}

enum OrderStatus {
  PLACED
  CANCELED
}

scalar Decimal
```

Describe inputs and payloads as well. Generated clients often surface these descriptions to application developers.

```graphql
"Cancels an order that has not entered fulfillment."
input CancelOrderInput {
  "The order to cancel."
  orderId: ID!

  "Customer-visible reason used in notifications and audit history."
  reason: OrderCancellationReason!
}

enum OrderCancellationReason {
  CUSTOMER_REQUEST
}
```

Descriptions should address:

| Contract detail | What to document |
| --- | --- |
| Units | Currency, time zone, percentages, or measurement units |
| Defaults | Sorting, filtering, paging, and server policies clients can rely on |
| Authorization | Whether the value may be absent because the viewer lacks access |
| Null semantics | Why a nullable field may return `null` even when execution succeeds |
| Similar fields | Which field clients should use and when |
| Deprecation | The replacement field or migration path |

If a description explains how to parse a string, JSON blob, or key/value payload, the schema may need named fields instead. If the description covers complex conditional behavior, consider using separate fields, required arguments, enums, input objects, or payload types.

For Hot Chocolate syntax, see [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation/). For deprecation and replacement guidance, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Choose fields, arguments, and types intentionally

Use GraphQL constructs to encode what clients can know at validation time. Do not leave constraints as runtime conventions if the schema can express them.

| Contract question | Likely construct | Review risk | Deeper page |
| --- | --- | --- | --- |
| Does the client read a value or relationship from an object? | Field on an object type | The field may expose storage shape instead of domain meaning. | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Does the client need a top-level read entry point? | Query field | The root `Query` type can become a list of screen or route variants. | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) |
| Does the client perform a side effect? | Mutation with input and payload types | Boolean or scalar results hide changed data and domain errors. | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) |
| Does a field need client-controlled refinement? | Arguments | An untyped bag of options hides meaning and validation. | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) |
| Do several input values move together? | Input object | Many scalar arguments become hard to evolve. | [Input object types](/docs/hotchocolate/v16/building-a-schema/input-object-types/) |
| Can the value be absent when execution succeeds? | Nullable or non-null type | Incorrect nullability creates either defensive clients or brittle responses. | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) |
| Can the list grow? | Paginated list with ordering semantics | Unbounded lists create cost and response-size risk. | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |
| Is the value one of a known set of states? | Enum | Enum additions can affect exhaustive generated client code. | [Enums](/docs/hotchocolate/v16/building-a-schema/enums/) |
| Do alternatives share fields clients should select consistently? | Interface | A weak shared contract can force fragments everywhere. | [Interfaces](/docs/hotchocolate/v16/building-a-schema/interfaces/) |
| Are alternatives disjoint result shapes? | Union | Clients need fragments for each possible object type. | [Unions](/docs/hotchocolate/v16/building-a-schema/unions/) |

Nullability describes the domain contract when execution succeeds. Execution errors can still produce `null` through GraphQL result completion and non-null propagation. For more, see [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

Use the schema to express constraints clients can understand:

- Use enums for closed sets like `OrderStatus`.
- Use custom scalars for semantic leaf values such as email, URL, currency, or date-only values when the distinction matters.
- Use default argument values only for defaults clients can rely on.
- Use required arguments, `@oneOf` input objects, or separate fields instead of runtime either/or validation.
- Use grouped object or input types for values that must move together.
- Reuse connection, edge, input, or payload types only when future evolution should intentionally be shared.

Aim to make invalid states unrepresentable where practical. Typed shapes are preferable to nullable bags, flag-heavy fields, loosely coupled arguments, and catch-all inputs.

# Prefer expressive schema over generic escape hatches

Generic shapes bypass GraphQL's typed contract. Avoid JSON blobs, loosely typed key/value maps, stringly typed enums, catch-all filter objects, and scalar payloads when the domain shape is knowable.

Before:

```graphql
scalar JSON

type Product {
  id: ID!
  metadata: JSON
}
```

After:

```graphql
scalar Decimal
scalar JSON

type Product {
  id: ID!
  dimensions: ProductDimensions
  materials: [ProductMaterial!]!
  metadata: JSON
}

type ProductDimensions {
  width: Decimal!
  height: Decimal!
  depth: Decimal!
  unit: LengthUnit!
}

enum LengthUnit {
  CENTIMETER
  INCH
}

type ProductMaterial {
  name: String!
}
```

The revised schema still includes `metadata`, but known concepts are now typed. Clients can validate operations, generate models, inspect descriptions, and review schema diffs. The remaining `metadata` field should have clear constraints and a description explaining what kind of partner or plugin data belongs there.

Escape hatches are sometimes appropriate for external passthrough data, analytics dimensions, plugin metadata, or intentionally unmodeled partner data. Use them sparingly, document them well, and keep them bounded. If a generic field starts to become a second API inside your API, replace known parts with fields, input fields, enums, object types, interfaces, unions, or payload types.

# Design for clients and generated tooling

Clients depend on stable type names, field names, argument names, nullability, enum values, IDs, and payload shapes. Generated clients turn schema changes into compile-time changes, so design choices appear directly in application code.

Input and payload objects make mutation contracts easier to expand. Stable object identity and predictable object types help normalized caches and generated model code. Descriptions and deprecation reasons show up in tooling, editors, and generated documentation.

Before publishing a schema area, review representative operations:

```graphql
query ProductDetails($id: ID!) {
  productById(id: $id) {
    id
    name
    price {
      amount
      currency
    }
    reviews(first: 3) {
      nodes {
        rating
        comment
      }
    }
  }
}
```

Then consider how changes will affect clients:

| Planned change | Generated-client impact | Design note |
| --- | --- | --- |
| Rename a field | Generated property and operation selections change. | Add a replacement field and deprecate the old one instead. |
| Loosen `String!` to `String` | Generated code may require new null checks. | Treat nullability changes as client-visible contract changes. |
| Add an enum value | Exhaustive switches may need updates. | Use stable domain states and document additions. |
| Add a nullable input field | Existing callers can omit it. | Good path for evolving input objects. |
| Add a payload field | Existing selections keep working. | Good path for returning more changed data. |
| Change a payload from scalar to object | Existing selections break. | Start with payload objects for mutations. |

Client-first design includes operation examples, fragments, variables, response shapes, and generated code review. See [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) for more on generated clients, fragments, normalized stores, and persisted operation workflows. For safe additions, dangerous changes, deprecation, and removal, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Review before publishing

Before publishing a new schema area, write a short review note. This note should demonstrate that the field belongs in the contract and that clients can use it safely.

Use this checklist:

| Review point | Question |
| --- | --- |
| Client task | Does each field support a real client task? |
| Domain meaning | Can someone understand the field without reading C# source? |
| Placement | Is the field an entry point, relationship, command, or specialized operation for a reason? |
| Exposure | Is the field safe to document, authorize, and support? |
| Cost | Are list size, data ownership, and resolver cost understood? |
| Arguments | Are arguments typed, named, and constrained by the schema where possible? |
| Nullability | Does nullable versus non-null match the domain and error behavior? |
| Descriptions | Do descriptions explain meaning, defaults, units, and migration guidance? |
| Inputs and payloads | Can mutation contracts evolve without breaking existing callers? |
| Abstract types | Are interfaces and unions chosen for the client selection model? |
| Generated clients | Can generated code and schema diff tools understand the change? |
| Evolution | Is there a path for future additions, deprecations, and removals? |

Nitro offers a practical review workflow. While not required for good design, it helps teams inspect the schema as clients will see it.

1. Inspect the generated SDL in [schema definition](/docs/nitro/documents/schema-definition/).
2. Review descriptions, deprecations, nullability, arguments, and enum values in [schema reference](/docs/nitro/documents/schema-reference/).
3. Run representative operations and review the selected response shape.
4. Validate schema changes against known client operations with the [schema registry](/docs/nitro/apis/schema-registry/) and [client registry](/docs/nitro/apis/client-registry/).
5. Move repeatable checks into local workflows or CI with the [Nitro schema commands](/docs/nitro/cli-commands/schema/) and [Nitro client commands](/docs/nitro/cli-commands/client/).

If the review raises a specialized question, continue with the relevant page:

| Question | Read next |
| --- | --- |
| Where should this field live? | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |
| How should data sources map into the contract? | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) |
| How will clients experience the change? | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Is the change safe to roll out? | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| How should failures appear? | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) |
| What should be nullable? | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) |
| Can the list grow? | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |
| What performance risk does selection create? | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) |

# Next steps

1. Read [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) to decide whether a field belongs on `Query`, an object relationship, or `Mutation`.
2. See [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) before choosing how to define the schema in Hot Chocolate.
3. Keep the [Building a schema](/docs/hotchocolate/v16/building-a-schema/) reference open for Hot Chocolate APIs for types, fields, arguments, nullability, interfaces, unions, and descriptions.
4. Use [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) before publishing changes that existing clients may already depend on.
