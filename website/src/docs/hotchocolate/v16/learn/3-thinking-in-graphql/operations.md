---
title: "Operations"
description: "Learn what a GraphQL operation is, how documents, operation names, variables, fragments, directives, and request envelopes work with Hot Chocolate v16."
---

A typical GraphQL request consists of three main components:

```json
{
  "query": "query GetProduct($id: ID!) { productById(id: $id) { id name price } }",
  "operationName": "GetProduct",
  "variables": {
    "id": "123"
  }
}
```

| Component           | Description                                                                 | Example                                 |
|---------------------|-----------------------------------------------------------------------------|-----------------------------------------|
| Operation document  | The GraphQL text containing `query`, `mutation`, or `subscription`          | `query GetProduct($id: ID!) { ... }`    |
| Operation name      | The name of the operation to execute (when multiple are present)            | `GetProduct`                            |
| Variables           | Input values provided at runtime, separate from the GraphQL text            | `{ "id": "123" }`                     |

The request envelope is the transport-level wrapper that carries these pieces. Over HTTP, this envelope is usually a JSON object with `query`, `operationName`, `variables`, and optionally `extensions`. Details like headers, HTTP methods, authentication, and content negotiation are part of [HTTP transport](/docs/hotchocolate/v16/server/http-transport/), not the operation document itself.

When Hot Chocolate receives a request, it parses the document, validates it against your schema, selects the operation, prepares variables, executes the selection set, and returns a result. The result includes `data` if execution succeeds, and may include `errors` if any issues occur.

This page explains the operation as the contract between client and server. Clients send operations, but the client itself is not the operation. For more on client-side concerns like schema discovery, code generation, caching, and request orchestration, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

# Documents, Operations, and Selections

According to the [GraphQL specification](https://spec.graphql.org/October2021/#sec-Language.Document), a document is a collection of definitions. In practice, these are usually:

- Operation definitions
- Fragment definitions used by operations

The structure can be visualized as:

```
Document
├─ Operation definition: GetProduct
│  └─ Selection set
│     ├─ Field: productById
│     └─ Nested fields: id, name, price
└─ Fragment definition: ProductSummary
   └─ Reusable selection set
```

An operation definition is the entry point for execution. It specifies the operation type, an optional name, optional variable definitions, and a selection set. Fragment definitions allow you to reuse selections, but fragments do not execute on their own. An operation must reference a fragment for its fields to be included.

A document may contain a single anonymous or named operation, or multiple named operations. If there are multiple, the request must specify `operationName` so Hot Chocolate knows which to run.

# Reading an Operation Document

Consider this example:

```graphql
query GetProduct($id: ID!, $withReviews: Boolean!) {
  product: productById(id: $id) {
    id
    name
    price
    reviews @include(if: $withReviews) {
      rating
      text
    }
  }
}
```

Breaking down the syntax:

| Syntax                        | Term                | Meaning                                                                 |
|-------------------------------|---------------------|-------------------------------------------------------------------------|
| `query`                       | Operation type      | Indicates this is a read operation                                      |
| `GetProduct`                  | Operation name      | Human-readable name for tools, logs, and `operationName`                |
| `($id: ID!, $withReviews: Boolean!)` | Variable definitions | Declares required variables for the operation                           |
| `{ product: productById(...) { ... } }` | Selection set        | Specifies which fields to fetch                                         |
| `id`, `name`, `price`         | Fields              | Fields to include in the response                                       |
| `id: $id`                     | Argument            | Passes the variable value as an argument                                |
| `product:`                    | Alias               | Changes the response key from `productById` to `product`                |
| `@include(if: $withReviews)`   | Directive           | Conditionally includes the `reviews` field                              |

The selection set determines the response shape. If `$withReviews` is `false`, the response will only include the selected fields that remain:

```json
{
  "data": {
    "product": {
      "id": "123",
      "name": "GraphQL Workshop",
      "price": 99.0
    }
  }
}
```

Aliases are useful for customizing response keys or fetching the same field with different arguments. The schema field remains `productById`, but the response uses `product` as the key. If you select the same field multiple times with different arguments, aliases keep the results distinct.

Directives like [`@include` and `@skip`](https://spec.graphql.org/October2021/#sec--include) allow for conditional selections. They do not make invalid fields valid; Hot Chocolate still validates all selections, arguments, fragments, and variable usage against the schema.

# Choosing the Operation Type

The operation type signals intent and affects execution:

| Use case                      | Operation type   | When to use                                      |
|-------------------------------|------------------|--------------------------------------------------|
| Load a product page           | `query`          | For reading data without changing state           |
| Submit an order               | `mutation`       | For changing state and returning a result         |
| Receive live order updates    | `subscription`   | For streaming results over time                   |

Use queries for reading data, mutations for state changes, and subscriptions for real-time updates when supported by the server.

Avoid hiding state changes behind queries, and do not use mutations for read-only data. This helps with client caching, operation review, and production policies. For more on schema design, see [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) and [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Using Variables

Variables allow you to keep the operation document stable while changing input values. For example:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
  }
}
```

With variables:

```json
{
  "id": "123"
}
```

This is preferable to embedding values directly in the operation:

```graphql
query GetProduct {
  productById(id: "123") {
    id
    name
  }
}
```

Variables help with validation, caching, generated clients, persisted operations, logs, and reviews. They also keep user input out of the GraphQL source text.

Variable definitions specify the expected GraphQL input type and nullability. Values are provided in the request envelope, and Hot Chocolate checks them before execution. For example, if `$id: ID!` is declared, the variables object must include a non-null `id`.

Do not construct operation strings to inject values or to change the number of variables for a list lookup. Prefer list variables when supported by the schema:

```graphql
query GetProducts($ids: [ID!]!) {
  productsById(ids: $ids) {
    id
    name
  }
}
```

With variables:

```json
{
  "ids": ["123", "456"]
}
```

If you find yourself using many dynamic aliases to fetch the same field with different IDs, consider whether a plural lookup field like `productsById(ids: [ID!]!)` would be clearer.

# Naming Operations

While operation names are optional for single-operation documents, naming them is recommended in real applications. Named operations help with:

- Logs, traces, and metrics
- Error reports and support tickets
- Persisted-operation and trusted-document workflows
- Generated client APIs
- Operation reporting and code review

When a document contains multiple operations, `operationName` selects which one to run:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
  }
}

query GetProductReviews($id: ID!) {
  productById(id: $id) {
    id
    reviews {
      rating
      text
    }
  }
}
```

Request example:

```json
{
  "query": "query GetProduct($id: ID!) { productById(id: $id) { id name } } query GetProductReviews($id: ID!) { productById(id: $id) { id reviews { rating text } } }",
  "operationName": "GetProduct",
  "variables": {
    "id": "123"
  }
}
```

If `operationName` is omitted in this case, Hot Chocolate cannot determine which operation to execute. For more on execution order, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/).

Choose operation names that reflect user intent, not only UI component names. For example, `GetProductForCheckout` is more descriptive than `ProductCardQuery` if the same component is used in multiple workflows.

# From Operation to Result

Hot Chocolate processes an operation request in several stages:

1. Parse the operation document
2. Validate against the schema
3. Select the requested operation
4. Coerce variables to the declared types
5. Execute the selected fields
6. Complete values into the response shape

For example:

```graphql
query GetProduct($id: ID!) {
  productById(id: $id) {
    id
    name
    price
  }
}
```

A successful result:

```json
{
  "data": {
    "productById": {
      "id": "123",
      "name": "GraphQL Workshop",
      "price": 99.0
    }
  }
}
```

If a resolver throws or reports an error, the response may include an `errors` array. Depending on nullability and where the error occurred, `data` may be partial, contain `null`, or be absent for request errors. For details, see [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

Fields not selected are not included in the response. Fields behind `@include` or `@skip` may be absent if the directive condition excludes them. Aliases change the response key from the schema field name.

# What Is and Is Not Part of the Operation

When debugging a request, it helps to know what belongs to the operation and what does not:

| Item                        | Belongs to           | More information                                                      |
|-----------------------------|----------------------|-----------------------------------------------------------------------|
| `query GetProduct`          | Operation document   | This page                                                            |
| `variables` JSON            | Request envelope     | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/)       |
| `operationName`             | Envelope/selection   | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) |
| `Accept` header             | HTTP transport       | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/)       |
| Generated C# client type    | Client tooling       | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Resolver method             | Server implementation| [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/)     |
| Query/mutation field design | Schema design        | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |

The client decides when and how to send operations. The transport delivers them. Hot Chocolate validates and executes them against the schema, and resolvers provide field values. Keeping these responsibilities distinct helps you troubleshoot issues.

# Persisted Operations

Persisted operations allow a client to send an operation identifier instead of the full document each time. Hot Chocolate uses the identifier to look up or verify the stored document, then executes it with the provided variables.

Variables are still sent with each execution, since their values change:

| Flow                        | Sent frequently                  | Remains stable                  |
|-----------------------------|----------------------------------|---------------------------------|
| Development request         | Operation document, name, variables | The schema contract             |
| Persisted operation request | Operation ID/hash, name (if needed), variables | The registered document         |

Do not confuse the operation hash with the operation name:

| Term             | Purpose                                                      |
|------------------|-------------------------------------------------------------|
| Operation hash/ID| Identifies the exact document text for lookup or storage    |
| Operation name   | Selects the operation definition and provides a label       |

The exact document text is important for persisted-operation lookup. Any change to whitespace, fields, fragments, aliases, or variable definitions can change the hash. Changing `operationName` selects a different operation if the document contains more than one.

Hot Chocolate supports [persisted operations (trusted documents)](/docs/hotchocolate/v16/performance/trusted-documents/) and [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/) for runtime negotiation. See those pages for configuration, storage, hash providers, and rollout details.

# Using Nitro to Author and Run Operations

Nitro provides a fast feedback loop for working with operation documents:

1. Open your schema in Nitro
2. Write a named operation (e.g., `GetProduct`)
3. Add variables in the variables panel
4. Add authentication headers if needed
5. Run the operation
6. Inspect `data`, `errors`, `extensions`, timing, and traces in the response
7. Use history to compare changes in documents, variables, and headers

Nitro helps with tasks such as:

| Task                                    | Nitro area                                                        |
|-----------------------------------------|-------------------------------------------------------------------|
| Compose selections with schema awareness| [Operation Pane](/docs/nitro/documents/operations/)               |
| Provide variables and headers           | [Operation Pane](/docs/nitro/documents/operations/) and [Authentication](/docs/nitro/documents/authentication/) |
| Inspect responses and history           | [Response Pane](/docs/nitro/documents/response/)                  |
| Review which named operations run       | [Operation Reporting](/docs/nitro/apis/operation-reporting/)      |

If an operation works in one environment but fails in another, compare the document, variables, endpoint, headers, schema version, and persisted-operation state before changing server code.

# Batching as a Transport Concern

Batching allows multiple executions to be grouped by the transport, but does not change the operation model. Documents, variables, operation selection, validation, and result shapes remain the same.

| Situation                                 | Model                                  |
|--------------------------------------------|----------------------------------------|
| One operation with one variables object    | Standard GraphQL request               |
| One operation executed with many variables | Variable batching                      |
| Multiple requests in one HTTP request      | Request batching                       |

For configuration, limits, streaming, and response correlation, see [Batching](/docs/hotchocolate/v16/server/batching/) and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/).

# Practice: Reading an Operation

Try reviewing this operation:

```graphql
query ReviewProductOperation($id: ID!, $withReviews: Boolean!) {
  product: productById(id: $id) {
    id
    name
    reviews @include(if: $withReviews) {
      rating
    }
  }
}
```

Check your understanding:

- The operation type is `query`.
- The operation name is `ReviewProductOperation`.
- Required variables are `id` and `withReviews`.
- The schema field `productById` appears as `product` in the response due to the alias.
- The `reviews` field is included only if `withReviews` is `true`.
- The variables object should look like:

```json
{
  "id": "123",
  "withReviews": true
}
```

If the document included another operation, the request would also need `"operationName": "ReviewProductOperation"`.

Before sending an operation to production, consider these questions:

| Question                                         | Why it matters                                                      |
|--------------------------------------------------|---------------------------------------------------------------------|
| Is the operation named?                          | Names help with diagnostics, APIs, and reporting                    |
| Are values passed through variables?             | Stable documents aid caching, validation, and reviews               |
| Does the operation type match the intent?        | Queries, mutations, and subscriptions have different expectations   |
| Can you predict the response shape?              | Predictable shapes help clients, tests, and debugging               |
| Are aliases used intentionally?                  | Aliases change response keys and can obscure schema field names     |
| Are directives used intentionally?               | Conditional selections may make fields absent in responses          |
| Does a multi-operation document provide a name?  | Hot Chocolate needs it to select the operation to execute           |

# Next Steps

- To learn about parsing, validation, operation selection, variable coercion, resolver execution, and result completion, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/).
- For client application concerns, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).
- For designing query and mutation fields, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) and [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/).
- For request envelopes, HTTP methods, headers, and content negotiation, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/draft/).
- For production operation contracts, see [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/) and [Automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations/).
- For batching multiple executions in one request, see [Batching](/docs/hotchocolate/v16/server/batching/).
