---
title: "Modeling entities vs operations"
description: "Decide whether a Hot Chocolate v16 schema field belongs on Query, on an object type, or on Mutation."
---

When designing a GraphQL schema, you often face a choice: should you expose a field like `latestOrdersForCustomer`, `orders(customerId:)`, or `customer.orders`? This decision comes up whenever you translate a client screen, service method, or database query into your schema. The approach you take affects how clients explore data, how resolvers are composed, and how much API surface you will need to maintain.

# Decide on the field shape before writing resolvers

Focus first on the contract's shape, not the resolver implementation.

For stable domain concepts, favor object types and navigable fields. Use root query fields as entry points into the graph. Place commands that change state on mutations. Keep operation-shaped query fields for searches, reports, workflow views, recommendations, aggregates, or other bounded read operations.

Consider this initial schema:

```graphql
type Query {
  latestOrdersForCustomer(customerId: ID!): [Order!]!
  customerOpenOrders(customerId: ID!): [Order!]!
  customerOrderHistory(customerId: ID!): [Order!]!
}

type Order {
  id: ID!
  number: String!
  status: OrderStatus!
}
```

Now compare it to this alternative:

```graphql
type Query {
  customerById(id: ID!): Customer
}

type Customer {
  id: ID!
  orders(first: Int, after: String, status: OrderStatus): OrderConnection!
}

type OrderConnection {
  edges: [OrderEdge!]!
  nodes: [Order!]!
  pageInfo: PageInfo!
}

type OrderEdge {
  cursor: String!
  node: Order!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}

type Order {
  id: ID!
  number: String!
  status: OrderStatus!
}

enum OrderStatus {
  OPEN
  SHIPPED
  CANCELED
}
```

The second schema provides a single entry point and a reusable relationship. This allows a customer detail page, a support tool, and a mobile order-history view to select different fields and arguments without requiring a new root field for each use case.

Before implementing a field, ask:

- Is this an entry point into the graph?
- Does this field belong on an object type because clients navigate from that object?
- Is this a command that causes side effects?
- Is this a bounded, operation-shaped read that does not fit a stable object relationship?

# Think in graphs, not endpoints

A GraphQL schema is a public contract made up of [types](https://spec.graphql.org/October2021/#sec-Types), fields, arguments, input objects, enum values, descriptions, and nullability. Clients send [operations](https://spec.graphql.org/October2021/#sec-Language.Operations) against this contract, but the schema should not be shaped one screen at a time.

Root fields act as entrances, while object fields are the paths clients follow:

```text
Query.bookById(id) -> Book.author -> Author.books
```

A schema for this path might look like:

```graphql
type Query {
  bookById(id: ID!): Book
}

type Book {
  id: ID!
  title: String!
  author: Author!
}

type Author {
  id: ID!
  name: String!
  books(first: Int, after: String): BookConnection!
}

type BookConnection {
  edges: [BookEdge!]!
  nodes: [Book!]!
  pageInfo: PageInfo!
}

type BookEdge {
  cursor: String!
  node: Book!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

Here, `bookById` is an entry point. `Book.author` and `Author.books` allow navigation through the graph. A client operation might look like this:

```graphql
query GetBook($id: ID!) {
  bookById(id: $id) {
    title
    author {
      name
      books(first: 3) {
        nodes {
          title
        }
      }
    }
  }
}
```

Queries are for reading data. Hot Chocolate executes query fields concurrently, so query resolvers must not perform writes. Place state-changing operations on `Mutation`, where top-level mutation fields run serially. For more on execution, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/). For operation syntax, variables, aliases, and selection sets, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/).

# Use entity-shaped fields for domain relationships

Choose an object field when the field represents a relationship or capability of the parent object.

Common examples include:

- `Author.books`
- `Customer.orders`
- `Issue.comments`
- `Product.reviews`
- `Organization.members`

Model relationships using domain language. Avoid copying database tables, EF navigation properties, service methods, or controller actions directly into the schema without considering whether clients should see that structure.

Prefer this approach:

```graphql
type Query {
  customerById(id: ID!): Customer
}

type Customer {
  id: ID!
  name: String!
  orders(first: Int, after: String, status: OrderStatus): OrderConnection!
}
```

Instead of this:

```graphql
type Query {
  getOrdersByCustomerId(customerId: ID!): [Order!]!
  getOpenOrdersByCustomerId(customerId: ID!): [Order!]!
  getOrdersForCustomerPage(customerId: ID!, page: String!): [Order!]!
}
```

Add arguments to the object field when they refine the relationship. For example, `Customer.orders(status:)` still means "orders for this customer," but the argument narrows the collection. If the list can grow, use pagination and set a clear maximum page size. Hot Chocolate supports cursor-based connections with `[UsePaging]`; see [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) and [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/).

Use nullability to describe the contract. If a customer might have no orders, an empty connection or list shows that the relationship exists but has no items. If the customer might not exist, the root lookup can return `Customer`. Review the nullability rules in [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and the [GraphQL null value rules](https://spec.graphql.org/October2021/#sec-Null-Value).

Entity-shaped fields often require careful data loading. Nested fields like `Order.customer` or `Product.brand` may run for many parent objects in a single operation. Plan for batching, projection, filtering, sorting, and resolver boundaries before publishing the schema. Start with [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/).

A good test is to write a client query for the field:

```graphql
query GetCustomerOrders($id: ID!) {
  customerById(id: $id) {
    name
    orders(first: 10, status: OPEN) {
      nodes {
        number
        status
      }
    }
  }
}
```

If the field name matches product language and can serve multiple client views, an entity-shaped field is likely the right choice.

# Use operation-shaped query fields for entry points and non-entity results

Operation-shaped query fields are appropriate in certain cases. They belong at the root when they serve as entry points or provide bounded read capabilities that are not relationships on a single parent object.

| User need | Preferred shape | Why | Follow-up docs |
| --- | --- | --- | --- |
| Fetch one object by stable identity | `productById(id: ID!): Product` or Relay `node(id: ID!): Node` | The client needs an entry point before navigating the object graph. | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/), [Relay](/docs/hotchocolate/v16/building-a-schema/relay/) |
| Search across many products | `searchProducts(input: ProductSearchInput!, first: Int, after: String): ProductSearchConnection!` | Search involves ranking, text matching, filters, and limits that are not properties of a single product. | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/) |
| Show a sales report | `salesReport(input: SalesReportInput!): SalesReport!` | A report is a generated read model, not a domain entity relationship. | [Public API guide](/docs/hotchocolate/v16/guides/public-api/) |
| List orders for one known customer | `Customer.orders(first:, after:, status:)` | The client already starts from a customer, so the field is a relationship. | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Show workflow state for a submission | `submissionReview(id: ID!): SubmissionReview` | The result may combine policy, state, and next actions for a bounded workflow view. | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |

Keep a field on `Query` when:

- There is no natural parent object
- The field spans multiple aggregate roots
- The result is generated, ranked, aggregated, or workflow-specific
- The field represents a bounded read capability with its own inputs and limits
- The field is an identity lookup or global refetch entry point

Move a field under an object when:

- It always starts from a single entity identity
- It repeats an existing relationship
- It adds clutter to the root for a single screen variant
- Its arguments refine a collection that already belongs to a parent object

Name operation-shaped fields after the result or capability, not after the service method. Use names like `salesReport`, `recommendedProducts`, or `searchProducts` instead of `runSalesReport`, `productRecommendationService`, or `searchProductsHandler`.

Operation-shaped fields often need additional safeguards. Searches, reports, recommendations, and aggregates may require pagination, maximum result sizes, cost analysis, caching, authorization, and clear semantics. Hot Chocolate provides request limits and cost analysis features for production APIs. See [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/) and [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/).

# Model mutations as commands with useful payloads

Mutations are top-level operations that cause side effects. Name each mutation as a command:

- `renameUser`
- `publishBook`
- `cancelOrder`
- `submitReview`

Prefer a single input object for the mutation. This gives the command a named shape and allows for future compatible additions.

```graphql
type Mutation {
  renameUser(input: RenameUserInput!): RenameUserPayload!
}

input RenameUserInput {
  userId: ID!
  newName: String!
}

type RenameUserPayload {
  user: User
  errors: [RenameUserError!]!
}

union RenameUserError = UserNotFoundError | UserNameTakenError

type UserNotFoundError {
  message: String!
}

type UserNameTakenError {
  message: String!
  suggestedName: String
}

type User {
  id: ID!
  name: String!
}
```

For most application mutations, return a payload object. This lets clients select data from the changed entity and handle domain errors in the same response.

```graphql
mutation RenameUser($input: RenameUserInput!) {
  renameUser(input: $input) {
    user {
      id
      name
    }
    errors {
      ... on UserNameTakenError {
        message
        suggestedName
      }
      ... on UserNotFoundError {
        message
      }
    }
  }
}
```

Use payload fields for domain results, such as changed entities, validation outcomes, permissions, or follow-up state. Use GraphQL execution errors for unexpected or request-level failures. For more on error boundaries, see [Error handling](/docs/hotchocolate/v16/guides/error-handling/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

Hot Chocolate mutation conventions can generate input and payload wrapper types for common resolver shapes. These reduce boilerplate, but always review the public schema they produce. See [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/#mutation-conventions).

Be cautious with required input fields. Required fields are part of the public contract. If a field might become optional for another workflow, or if a future version could use an alternative value, model that intentionally before clients depend on it.

# Review common modeling decisions

Use this table during design reviews:

| User need | Likely shape | Trade-off | Verification question |
| --- | --- | --- | --- |
| Fetch one item from outside the graph | Root identity lookup, such as `orderById(id:)` | Adds a root field, but gives clients a clear starting point. | Can clients refetch this object by stable identity? |
| Fetch a child collection | Object field with pagination, such as `Customer.orders(first:, after:)` | Creates nested resolver and data-loading work. | Does the field read as a relationship of the parent object? |
| Search many records | Root search field with input and paging | Keeps search semantics explicit, but needs ranking, limits, and cost controls. | Is there a natural parent, or is this a cross-domain capability? |
| Show a dashboard | Operation-shaped read model | Serves a task well, but can become screen-specific if unmanaged. | Is the result a stable business view rather than one UI component? |
| Move workflow state forward | Mutation command | Communicates side effects, but needs useful payload and domain errors. | What changed, and what should the client select next? |
| Read workflow state | Query field or object field | Keeps reads separate from transitions. | Does the field change state when selected? |
| Count or aggregate scoped data | Field on the relevant object, such as `Customer.orderCount` | Convenient for clients, but may be expensive when selected in lists. | Can you compute it efficiently for many parents? |
| Count or aggregate across domains | Root field, such as `salesSummary(input:)` | Exposes a bounded report capability. | What limits, cache policy, and authorization apply? |

Each modeling choice affects public API stability. Names, descriptions, nullability, argument defaults, input object fields, payload fields, and deprecations all impact generated clients and saved operations. Use descriptions to clarify meaning, and deprecate fields instead of renaming them after adoption. For more on production review, see [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Check for performance and evolution pressure before publishing

Before publishing, review each new root field, nested relationship, and mutation for these concerns:

| Pressure | What to check | Useful docs |
| --- | --- | --- |
| Nested data access | Could this create N+1 queries when selected under a list? | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/), [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) |
| Provider translation | Should paging, filtering, sorting, or projection run in the database? | [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/) |
| Large lists | Does the field require pagination and maximum page sizes? | [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) |
| Expensive computation | Does the field need limits, cost metadata, caching, or a separate operation-shaped read? | [Request limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/) |
| Public contract | Are names, descriptions, arguments, input fields, payload fields, and nullability stable? | [Public API guide](/docs/hotchocolate/v16/guides/public-api/) |
| Client impact | What becomes breaking after generated clients or persisted operations adopt the field? | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/), [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |

Inspect the generated schema before publishing. Nitro's [Schema Reference](/docs/nitro/documents/schema-reference/) and [Schema Definition](/docs/nitro/documents/schema-definition/) help you review root fields, object relationships, mutation payloads, arguments, pagination fields, and nullability from the client's perspective.

A short review note should answer:

- Chosen shape: root query, object field, or mutation
- Reason: entry point, relationship, bounded read, or command
- Paging decision: connection, bounded list, or no list
- Cost concern: data loading, provider translation, report cost, or none
- Evolution note: what can be added later without breaking clients

# Field design checklist

Before publishing, run this checklist for each new root field and mutation:

- Is this a read or a side effect?
- Is it an entry point, an object relationship, an operation-shaped read, or a command?
- Does the name use domain language instead of storage, service, transport, or UI terms?
- Can clients select the follow-up data they need?
- Does the list need pagination, filtering, sorting, or maximum limits?
- Does nesting create data-loading work?
- Can the input object evolve without breaking existing clients?
- Does the payload expose changed entities and domain errors when the operation changes state?
- Does nullability match what the server can guarantee?
- Have you inspected the generated schema in Nitro or SDL?
- What would be breaking after clients adopt this field?

# Next steps

- Review broader contract principles in [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/).
- Learn about operation documents, variables, and selection sets in [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/).
- Implement root fields with [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) and [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/).
- Plan list and nested-field execution with [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/).
