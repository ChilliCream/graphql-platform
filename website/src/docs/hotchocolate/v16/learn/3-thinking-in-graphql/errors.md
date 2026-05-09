---
title: "Errors"
description: "Design GraphQL errors, domain failures, mutation payload errors, and safe exception boundaries for Hot Chocolate v16 APIs."
---

GraphQL responses can include both `data` and `errors` in the same payload. This is not a contradiction: it reflects that some parts of an operation may succeed while others do not. Understanding where to surface each type of failure is essential for building robust APIs.

Use the top-level `errors` array for issues related to request validation, execution, infrastructure, or unexpected field failures. For business outcomes that clients are expected to handle, represent them as typed schema data.

# Distinguishing GraphQL Errors from Domain Outcomes

When a mutation is rejected, you might wonder why the API returns an error even though the server enforced a business rule. The distinction lies in the nature of the error:

- **GraphQL errors** indicate that GraphQL could not fully execute or complete the requested selection. Examples include:
  - Selecting a field not present in the schema.
  - A resolver throwing due to a failed database connection.
  - An upstream service timing out during field resolution.
- **Domain outcomes** describe why a business action did not occur. Examples include:
  - The username is already taken.
  - The order cannot be cancelled because it has shipped.
  - The account lacks sufficient funds for a transfer.

Domain outcomes should be modeled in the schema. This allows clients to query, generate types, test, and document these outcomes. If every business rejection is surfaced as a top-level GraphQL error, clients are forced to parse error messages or rely on undocumented codes outside the schema.

A helpful guideline: treat `database unavailable` as a top-level error, but represent `username taken` as part of the mutation payload data.

# Routing Failures by Intent

Before choosing a Hot Chocolate API, classify the type of failure:

| What happened? | Where should it appear? | What should the client do? | Hot Chocolate feature |
| --- | --- | --- | --- |
| Invalid GraphQL document | Top-level `errors` as a request error | Fix the operation document | Request validation |
| Invalid variable shape | Top-level `errors` as a request error | Fix the request variables | Input coercion |
| Resolver throws during a field | Top-level `errors` as a field/execution error | Use remaining data if possible, log, retry where safe | Error pipeline, `GraphQLException`, `ReportError`, filters |
| Database or service timeout | Top-level `errors` as a field/execution error | Show a service problem, retry where safe, alert operators | Error pipeline and instrumentation |
| Declared `UserNameTakenException` on a mutation | Payload `errors` field | Show a form or business-rule message | Mutation conventions and `[Error]` |
| Declared `OrderAlreadyShippedException` on a mutation | Payload `errors` field | Explain the current business state | Mutation conventions and `[Error]` |

Authentication and [authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) are typically handled as request or field failures. However, some products may choose to model policy outcomes as domain data, especially if the client needs to present a specific recovery path. Let the intended client action guide your schema design.

Not every thrown exception is a domain error. With Hot Chocolate mutation conventions, only declared domain exceptions are mapped into payload error objects. Undeclared exceptions are treated as runtime errors and flow through the standard GraphQL error pipeline.

Transport-level errors are handled separately. Issues with HTTP methods, status codes, or content negotiation occur outside GraphQL execution and may not produce a standard `data`/`errors` envelope. Clients should always check the HTTP status and response media type before interpreting GraphQL results. For more on status codes and content negotiation, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport/).

# Understanding the Top-Level `errors` Array

The [GraphQL response format](https://spec.graphql.org/October2021/#sec-Response-Format) allows a response to include `data`, `errors`, and optional `extensions`.

A typical top-level error includes:

| Field | Meaning | Client contract |
| --- | --- | --- |
| `message` | Human-readable text | Do not treat as a stable API contract. |
| `path` | The response path affected by a field error | Use to identify which field failed. |
| `locations` | Positions in the operation document | Useful for debugging the operation text. |
| `extensions.code` | Machine-readable code, if provided | Use stable codes for technical branching when defined by your API. |

Do not rely on the order of errors. Remember, top-level errors are not part of the schema, so clients cannot discover domain-specific top-level errors through introspection as they can with typed payload errors.

For example, consider a client querying for a book and its author:

```graphql
query GetBook($id: ID!) {
  bookById(id: $id) {
    title
    author {
      name
    }
  }
}
```

If the author service is unavailable, and the schema allows it, Hot Chocolate can still return the book title:

```json
{
  "data": {
    "bookById": {
      "title": "GraphQL in Practice",
      "author": null
    }
  },
  "errors": [
    {
      "message": "The author service is unavailable.",
      "path": ["bookById", "author"],
      "extensions": {
        "code": "AUTHOR_SERVICE_UNAVAILABLE"
      }
    }
  ]
}
```

The `path` property identifies which field failed, while the remaining `data` shows what is still available to the client.

Use [Nitro operations](/docs/nitro/documents/operations/) and the [Nitro response pane](/docs/nitro/documents/response/) to inspect the full envelope, including partial `data`, `errors`, paths, extensions, status, duration, and response history.

# Distinguishing Infrastructure Failures from Business Rejections

Infrastructure failures occur when the system cannot complete the operation as requested. Business rejections mean the system processed the command, but the answer was no.

| Scenario | Classification | Response surface | Client behavior |
| --- | --- | --- | --- |
| Payment provider timed out | Infrastructure failure | Top-level `errors` | Show a service problem, retry where safe, report the incident. |
| Card was declined | Business rejection | Mutation payload error | Ask the user for another payment method. |
| Database transaction failed | Infrastructure failure | Top-level `errors` | Avoid claiming success, log, and retry only when safe. |
| Order has already shipped | Business rejection | Mutation payload error | Explain that cancellation is no longer available. |
| User service unavailable | Infrastructure failure | Top-level `errors` | Show a temporary service problem. |
| Username is already taken | Business rejection | Mutation payload error | Show a form error and ask for another username. |

Technical failures should be logged, observed, retried when appropriate, and sanitized in the response. Business rejections should use names, fields, and codes that reflect the product language.

Never expose exception type names, stack traces, SQL messages, downstream service names, HTTP status text, or infrastructure details in domain error types. These belong in server logs, traces, and operational tools.

# Modeling Expected Domain Errors in Mutation Payloads

Mutations represent commands, and commands can have valid negative outcomes. The payload object is the result of the command and can include both changed data and domain errors. For example, if a user tries to update a username and the name is already taken, GraphQL execution may have succeeded, but the business command returned a negative result.

Model these outcomes as schema data:

```graphql
type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

interface Error {
  message: String!
  code: String
}

type UserNameTakenError implements Error {
  message: String!
  code: String
  username: String!
}

type InvalidUserNameError implements Error {
  message: String!
  code: String
  field: String!
}

union UpdateUserNameError = UserNameTakenError | InvalidUserNameError
```

Clients must explicitly select the payload error fields:

```graphql
mutation UpdateUserName($input: UpdateUserNameInput!) {
  updateUserName(input: $input) {
    user {
      id
      username
    }
    errors {
      __typename
      ... on Error {
        message
        code
      }
      ... on UserNameTakenError {
        username
      }
      ... on InvalidUserNameError {
        field
      }
    }
  }
}
```

If the client does not select the `errors` field, those domain errors will not appear in the response. This follows the standard GraphQL selection-set rule: domain errors are data.

Unexpected runtime failures, such as a database outage while saving the new username, should still be reported in the top-level `errors` array.

# Why the Error Payload Pattern Is Effective

The error payload pattern makes known mutation outcomes first-class schema types. Marc-Andre Giroux describes this approach in [A Guide to GraphQL Errors](https://magiroux.com/posts/guide-to-graphql-errors), including the Stage 6a-style typed mutation error pattern.

Each part of the pattern serves a purpose:

| Piece | Why it exists |
| --- | --- |
| Payload object | Provides a single, evolvable result object for the mutation. |
| Payload `errors` field | Keeps known negative outcomes next to the command they relate to. |
| Error union | Lists the concrete domain error types possible for that mutation. |
| Shared error interface | Offers common fields such as `message`, `code`, `field`, or recovery hints. |
| Concrete error object | Carries domain-specific data like `username`, `minimumAge`, `availableStock`, or `currentOrderState`. |

Without typed payload errors, a client might receive:

```json
{
  "errors": [
    {
      "message": "Username is already taken"
    }
  ]
}
```

In this case, the client must parse the message or rely on a code outside the schema. With typed payload errors, the client can branch on `__typename` and access structured fields:

```json
{
  "data": {
    "updateUserName": {
      "user": null,
      "errors": [
        {
          "__typename": "UserNameTakenError",
          "message": "Username is already taken.",
          "code": "USERNAME_TAKEN",
          "username": "ada"
        }
      ]
    }
  }
}
```

Generated clients can create typed branches for each error type. Inline fragments, fragments, and data masking keep these requirements visible in the operation. For more on client-side response handling, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

Payload error lists are effective when a mutation has a standard payload plus zero or more domain rejections for form fields, command validation, or business rules. If a mutation can return one of several mutually exclusive result objects, a result union may be a better fit, allowing the client to branch on the entire result.

# What Mutation Conventions Generate

Hot Chocolate mutation conventions reduce boilerplate while preserving the schema pattern. They can wrap mutation arguments in an input object and results in a payload object. The generated schema remains the public contract.

For a method named `UpdateUserNameAsync`, conventions can produce a schema like:

```graphql
type Mutation {
  updateUserName(input: UpdateUserNameInput!): UpdateUserNamePayload!
}

input UpdateUserNameInput {
  userId: ID!
  username: String!
}

type UpdateUserNamePayload {
  user: User
}
```

When you declare domain exceptions with `[Error]`, Hot Chocolate adds an `errors` field to the payload and maps declared exceptions into schema error object types:

```graphql
type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

interface Error {
  message: String!
}

type UserNameTakenError implements Error {
  message: String!
}

type InvalidUserNameError implements Error {
  message: String!
}

union UpdateUserNameError = UserNameTakenError | InvalidUserNameError
```

By default, exception-style names are rewritten as schema error names. For example, `UserNameTakenException` becomes `UserNameTakenError`.

Undeclared exceptions remain runtime errors and are surfaced through the standard GraphQL error pipeline. Error factories and custom error interfaces allow you to control the public shape. For implementation details, see [Mutations and mutation conventions](/docs/hotchocolate/v16/building-a-schema/mutations/).

# Designing Domain Error Types for Clients

Design domain errors as API types, not as exception wrappers. Begin with the client action you want to support.

| Error type | When it appears | Useful fields | Client action |
| --- | --- | --- | --- |
| `UserNameTakenError` | Requested username already exists | `message`, `code`, `username` | Highlight the username field and prompt for another value. |
| `OrderAlreadyShippedError` | User tries to cancel a shipped order | `message`, `code`, `currentState` | Explain why cancellation is unavailable and refresh order state. |
| `InsufficientInventoryError` | Requested quantity exceeds stock | `message`, `code`, `requestedQuantity`, `availableQuantity` | Show available quantity or offer backorder. |
| `InvalidTransferAmountError` | Transfer amount is outside policy | `message`, `code`, `minimumAmount`, `maximumAmount`, `submittedAmount` | Show the allowed range. |

Use business domain names such as `UserNameTakenError`, `OrderAlreadyShippedError`, and `InsufficientInventoryError`. Avoid exposing server internals in public names.

Choose shared fields intentionally:

- `message` for safe user-facing or developer-facing text.
- `code` for stable cross-operation branching.
- `field` or `inputPath` for mapping errors to form fields.
- Recovery hints when multiple error types share the same next action.

Add concrete fields when they influence the client’s response. For example, a range error can include allowed minimum and maximum values, an inventory error can include requested and available quantities, and an invalid state transition can include the current and allowed next states.

Avoid making clients parse the `message` field. Keep messages safe, concise, and actionable. When adding, renaming, or testing error contracts, refer to [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/).

# Handling Partial Data and Null Propagation

GraphQL allows `data` and top-level `errors` to appear together because each selected field is completed according to the schema type.

A nullable field can become `null` if a field error occurs. If a non-null field cannot be completed, null propagation occurs up to the nearest nullable parent, which may remove more data than the original field that failed.

Consider this schema:

```graphql
type Query {
  order(id: ID!): Order
}

type Order {
  id: ID!
  payment: Payment!
}

type Payment {
  status: String!
}
```

If `Payment.status` cannot be resolved, GraphQL cannot return `null` for `status` (it is non-null), nor for `payment` (also non-null). The nearest nullable parent is `order`, so `data.order` becomes `null`, even though the error path points to `order.payment.status`:

```json
{
  "data": {
    "order": null
  },
  "errors": [
    {
      "message": "Payment status could not be resolved.",
      "path": ["order", "payment", "status"]
    }
  ]
}
```

Nullability is part of error design, not only type syntax. Do not use a null payload plus a top-level error to represent an expected business command failure. Instead, return a payload with domain error data.

For a detailed explanation of nullability, see [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/). For more on execution phases, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/).

# Keeping Exception Details Secure

Exception details can be useful during development, but production systems should log exceptions server-side and return only sanitized messages or stable codes.

By default, Hot Chocolate hides exception details unless a debugger is attached. You can enable `IncludeExceptionDetails` for development:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(
        o => o.IncludeExceptionDetails =
            builder.Environment.IsDevelopment());
```

Never enable exception details in production. Stack traces, SQL messages, HTTP responses from downstream services, and internal exception text can expose sensitive information.

Use error filters to translate or enrich top-level GraphQL errors, but do not use them as a substitute for typed domain payloads. Domain errors in payloads are schema data, so update the domain error type, factory, or mutation convention configuration to change that contract.

Clients receive safe contracts, while operators get diagnostic details through logs, traces, metrics, and monitoring. For server diagnostics, see [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/). For production operation failures, see [Nitro operation monitoring](/docs/nitro/open-telemetry/operation-monitoring/).

For details on `IError`, `ErrorBuilder`, `GraphQLException`, `IResolverContext.ReportError`, `IErrorFilter`, `AddErrorFilter`, and exception detail options, see the [Errors API reference](/docs/hotchocolate/v16/api-reference/errors/).

# Using Top-Level Errors Outside Mutations

Not every failure should be placed in a payload error. Queries and subscriptions do not use mutation conventions, and not every missing value is an error.

For query fields, consider what the client needs to know:

| Situation | Good response shape |
| --- | --- |
| Normal absence, such as no search results | Empty list or nullable data |
| Expected product state that drives UI | Typed result union |
| Technical field failure, such as an author service timeout | Top-level field error |
| Authorization or policy failure | Request or field error, unless modeled as domain data |

For example, a product lookup can use a union when each outcome represents a business state the client should handle:

```graphql
type Product {
  id: ID!
  name: String!
}

type DiscontinuedProduct {
  id: ID!
  name: String!
  discontinuedAt: String!
}

type ProductUnavailableInRegion {
  id: ID!
  name: String!
  region: String!
}

union ProductLookupResult =
  Product
  | DiscontinuedProduct
  | ProductUnavailableInRegion
```

A catalog search, on the other hand, can return an empty list when there are no matches. An author service timeout remains a top-level field error because GraphQL could not complete the selected field.

Use `ReportError` or `GraphQLException` for technical field errors when needed. Use result unions for expected query outcomes that clients should handle as typed domain results. Use nullable data, empty lists, or standard not-found behavior when absence is part of the contract.

For more on command and result design, see [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/).

# Next Steps

- For expected domain rejections in mutations, see [Mutations and mutation conventions](/docs/hotchocolate/v16/building-a-schema/mutations/).
- If a resolver throws unexpectedly, see the [Errors API reference](/docs/hotchocolate/v16/api-reference/errors/) and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/).
- If nulls are surprising, see [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/).
- For the execution model, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/).
- To inspect the response envelope from a client perspective, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/), [Nitro operations](/docs/nitro/documents/operations/), and the [Nitro response pane](/docs/nitro/documents/response/).
- For transport status-code behavior and content negotiation, see [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/).
- If security policy changes the result, see [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) and decide whether the client needs a policy failure or a domain outcome.
- For testing error contracts, see [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/).
- For evolving error types or codes, see [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).
- For broader context on GraphQL error design, see [A Guide to GraphQL Errors](https://magiroux.com/posts/guide-to-graphql-errors).
