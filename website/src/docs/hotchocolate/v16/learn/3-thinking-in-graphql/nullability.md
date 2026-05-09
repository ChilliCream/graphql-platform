---
title: "Nullability"
description: "Design GraphQL nullability in Hot Chocolate v16 as an API contract for successful data, execution errors, lists, inputs, and generated clients."
---

```graphql
type Product {
  name: String!
  description: String
  averageRating: Float
}
```

When designing GraphQL nullability, the key question is not whether your C# code can return `null`, but whether a value might be absent when the operation succeeds. In GraphQL, fields are nullable by default unless you mark them as non-null with `!`. A nullable field allows `null` as a valid response, while a non-null field means `null` is not a valid domain value.

Base your nullability decisions on the meaning of the value. For example, if every product must have a display name, use `name: String!`. If a product might not have a description, use `description: String`. If a product with no reviews has no rating yet, use `averageRating: Float`.

This distinction helps clients know whether they can rely on a field being present when no execution error has occurred.

# Distinguishing semantic nulls from error nulls

A GraphQL response can include `null` for several reasons:

| Null source | Example | What the client should learn |
| --- | --- | --- |
| Domain absence | `description: null` for a product without marketing copy | The value is absent and that absence is valid. |
| Redaction | `manufacturerEmail: null` for a viewer without permission | The value exists, but this viewer cannot see it. |
| Optional data unavailable | `estimatedDeliveryDate: null` before the carrier provides one | The value is not known yet. |
| Execution error replacement | `name: null` with an error path of `["product", "name"]` | The field failed during execution. |
| Null propagation | `product: null` because `product.name: String!` failed | A non-null child could not complete under the error policy. |

**Semantic nullability** refers to the domain reality: can this value be `null` when no execution error has occurred?

Avoid making a field nullable only because a resolver, database, or downstream service might fail. Doing so widens generated client types and forces unnecessary null checks, even when the value is required by the domain.

Use nullability to describe successful data. Use error handling for execution failures. The GraphQL schema is the contract your clients observe, regardless of how Hot Chocolate infers it from C#.

# Traditional null propagation in GraphQL

The [GraphQL specification](https://spec.graphql.org/October2021/#sec-Handling-Field-Errors) defines how field errors and result completion work. A response can include both partial `data` and an `errors` array.

Consider this schema:

```graphql
type Query {
  product(id: ID!): Product
}

type Product {
  name: String!
  description: String
}
```

And this query:

```graphql
query GetProduct($id: ID!) {
  product(id: $id) {
    name
    description
  }
}
```

If an execution error occurs in `description`, the field can be `null` and the response still includes `name`, because `description` is nullable:

```json
{
  "data": {
    "product": {
      "name": "Trail Shoe",
      "description": null
    }
  },
  "errors": [
    {
      "message": "The description could not be loaded.",
      "path": ["product", "description"]
    }
  ]
}
```

If `name: String!` fails or returns `null`, traditional propagation does not allow `null` at `product.name`. Instead, `null` moves up to the nearest nullable parent. In this schema, `product` is nullable, so the response becomes:

```json
{
  "data": {
    "product": null
  },
  "errors": [
    {
      "message": "The name could not be loaded.",
      "path": ["product", "name"]
    }
  ]
}
```

The error path still points to the field that failed. If all parents are non-null, the top-level `data` can become `null`.

Traditional propagation maintains the non-null guarantee, but it can remove sibling data that would otherwise be available. For more on execution, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) and for error handling, see [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).

# Controlling propagation with `onError`

Hot Chocolate v16 supports the GraphQL `onError` request property, which can be set to `PROPAGATE` or `NULL`. This follows the direction of the GraphQL `onError` proposal and separates semantic nullability from error propagation.

```json
{
  "query": "query GetProduct($id: ID!) { product(id: $id) { name description } }",
  "operationName": "GetProduct",
  "variables": {
    "id": "UHJvZHVjdDox"
  },
  "onError": "NULL"
}
```

| Mode | Behavior when a non-null field errors | Result shape |
| --- | --- | --- |
| `PROPAGATE` | Traditional null propagation. | `product` may become `null` if `product.name` errors. |
| `NULL` | Error stays at the field position. | `product.name` can be `null`, the error is in `errors`, and `product.description` can still be present. |

With `onError: "NULL"`, a failure in `name` preserves the parent object and sibling fields:

```json
{
  "data": {
    "product": {
      "name": null,
      "description": "Waterproof trail shoe."
    }
  },
  "errors": [
    {
      "message": "The name could not be loaded.",
      "path": ["product", "name"]
    }
  ]
}
```

This approach allows you to keep `name: String!` when `null` is not a valid successful value, even though execution errors can still occur.

Hot Chocolate defaults to `PROPAGATE`. You can change the server default and allow requests to override it:

```csharp
// Program.cs
using HotChocolate.Language;

builder
    .Services
    .AddGraphQLServer()
    .ModifyRequestOptions(options =>
    {
        options.DefaultErrorHandlingMode = ErrorHandlingMode.Null;
        options.AllowErrorHandlingModeOverride = true;
    });
```

Align configuration with client expectations. `onError: "NULL"` preserves more data, but clients must treat field errors as errors, not as business absence. For related configuration, see the [options reference](/docs/hotchocolate/v16/api-reference/options/). For migration details, see [Experimental @semanticNonNull support removed](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16/#experimental-semanticnonnull-support-removed).

# Client error handling with `onError: "NULL"`

When the server does not propagate a non-null field error to its parent, clients must not treat the errored `null` as a normal nullable value.

Clients can handle field errors in several ways:

| Client strategy | What happens | When to use |
| --- | --- | --- |
| Coerce to null | The field value is `null`, and application code checks `errors` or nullable values. | For clients that already model partial data this way. |
| Throw on field error | Reading an errored field throws or routes to an error boundary. | When UI code should treat semantic non-null fields as present unless they errored. |
| Result wrapper | Generated code exposes a value-or-error shape near the field. | When the app wants local handling for selected fields without exceptions. |

Relay documents this with [`@throwOnFieldError`](https://relay.dev/docs/guides/semantic-nullability/) and semantic nullability. Apollo Kotlin describes similar behavior with [`@semanticNonNull` and `@catch`](https://www.apollographql.com/docs/kotlin/advanced/nullability).

You do not need to use those clients to apply this pattern. The key point: if `onError: "NULL"` keeps the parent data, the client still needs an error policy for affected fields. Generated client types can then reflect the domain more closely, since required fields are not made nullable only to represent possible execution errors.

For more on client responsibilities, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

# Choosing schema nullability based on domain guarantees

Use this table to guide your field review:

| Scenario | Preferred contract | Reason |
| --- | --- | --- |
| Required display name | `name: String!` | A product without a name is invalid. |
| Optional description | `description: String` | Absence is part of the product model. |
| Product lookup by ID may not find a product | `product(id: ID!): Product`, a domain result union, or a typed payload | Choose based on whether "not found" is absence, a business result, or an error for that entry point. |
| Product with no reviews has no rating | `averageRating: Float` or a structured aggregate | `null` can mean no rating yet, or the aggregate can expose `reviewCount` and `average`. |
| Field backed by a flaky downstream service | Keep the semantic contract, then choose `onError` behavior | Service failure is an execution concern, not proof that the value is optional. |
| Redacted value | Nullable field with a clear description, or a shape that exposes visibility | The viewer may validly receive no value. |

Use non-null when `null` is not a valid successful value. Use nullable when absence is part of the domain, such as optional profile text, no rating yet, redacted data, or not applicable data. If you need a non-null enum with an "unknown" or "not applicable" state, model that state as an explicit enum value instead of using GraphQL `null`.

Model business outcomes separately from nullability. Mutation payloads often need typed domain errors instead of a nullable scalar or Boolean:

```graphql
type Mutation {
  cancelOrder(input: CancelOrderInput!): CancelOrderPayload!
}

type CancelOrderPayload {
  order: Order
  errors: [CancelOrderError!]!
}
```

The nullable `order` means a successful mutation payload may not contain an order when a domain error occurred. The `errors` list provides the business reason. For Hot Chocolate mutation conventions, see [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/).

Be careful with non-null fields backed by remote services or associations if your clients cannot handle field errors or `onError` safely. The schema can still express domain truth, but rollout requires a matching client error policy.

Treat every nullability change as a schema evolution decision. Generated client types, validation, and UI logic depend on it.

# List nullability: container and items

Lists in GraphQL have two layers of nullability:

1. The list container
2. Each item in the list

```graphql
type Product {
  reviews: [Review!]!
  relatedProducts: [Product!]
  recommendationSlots: [Product]!
}
```

| GraphQL type | Container | Items | Meaning |
| --- | --- | --- | --- |
| `[Review!]!` | Non-null | Non-null | Every product has a list, and every item is a review. |
| `[Review!]` | Nullable | Non-null | The list itself may be unavailable, hidden, or not applicable. |
| `[Review]!` | Non-null | Nullable | The list exists, but some positions may be `null`. |
| `[Review]` | Nullable | Nullable | Both the list and items may be `null`. |

An empty list is not the same as a `null` list:

```json
{
  "data": {
    "product": {
      "reviews": []
    }
  }
}
```

Use `[]` when the relationship exists but has no items. Use a nullable list only when the relationship itself might not apply, be hidden, or unavailable. Nullable items should be rare, unless a `null` position has meaning, such as a fixed recommendation slot that could not be filled.

Execution errors in list items make error propagation especially visible. With traditional `PROPAGATE` behavior, an error at a non-null item position cannot become a `null` element. The error propagates to the list or the nearest nullable parent, depending on the wrapping types. Under `PROPAGATE`, a `null` element appears only if the item type is nullable. With `onError: "NULL"`, the errored position stays local and `errors[].path` identifies the element.

For details on collection mapping and descriptor overrides, see [Lists](/docs/hotchocolate/v16/building-a-schema/lists/) and [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null/).

# Input nullability: caller obligations

Output nullability answers what the server may return. Input nullability answers what the client must provide.

```graphql
type Query {
  product(id: ID!): Product
  searchProducts(text: String, category: ID): [Product!]!
  searchProductsWithDefault(text: String = ""): [Product!]!
}
```

`product(id: ID!)` requires the caller to provide `id`. If the client omits `id` or passes `null`, the operation fails before field execution.

Nullable arguments allow omission or explicit `null` unless other validation applies. Omitted values and explicit `null` can have different meanings in resolver code and input object coercion, so document the behavior clients can rely on.

Use default values only when the server has a real default behavior. `text: String = ""` means the empty search text is an intentional default, not a workaround for optional input.

Adding a new required argument or required input field breaks existing operations that do not provide it. Prefer required arguments, separate fields, or `@oneOf` input objects over runtime either-or validation when the schema can express the caller obligation.

`onError` applies to execution error handling for response data. It does not replace input validation rules. For implementation details, see [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) and [Input object types](/docs/hotchocolate/v16/building-a-schema/input-object-types/).

# C# nullability as an implementation detail

Hot Chocolate can infer GraphQL nullability from C# nullable reference types and value types. New .NET projects should enable nullable reference types so C# annotations help express the intended schema contract.

```csharp
public sealed class Product
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IReadOnlyList<Review> Reviews { get; set; } = [];
}
```

With nullable reference types enabled, this shape typically produces:

```graphql
type Product {
  name: String!
  description: String
  reviews: [Review!]!
}
```

| C# authoring signal | GraphQL shape | Review habit |
| --- | --- | --- |
| `string` | `String!` | Verify this is a domain guarantee, not a storage accident. |
| `string?` | `String` | Document what valid absence means. |
| `IReadOnlyList<Review>` | `[Review!]!` | Check both the container and item contract. |
| `IReadOnlyList<Review?>` | `[Review]!` | Confirm a `null` item has meaning. |

The generated SDL remains the public contract. Client tooling consumes the GraphQL schema, not your C# source. Inspect the SDL after changing C# annotations, attributes, descriptor configuration, or collection types.

Use [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null/) for detailed C# mapping, attributes, and descriptor APIs. Use [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) when you need broader schema authoring control.

# Verify the contract clients receive

After you change nullability, review the schema and the response behavior together.

| Review question | Artifact to inspect |
| --- | --- |
| What does `null` mean for this field? | Field description, SDL, and product requirements. |
| Is `!` expressing domain truth? | Generated SDL and resolver contract. |
| What happens if the resolver errors? | Response `data`, `errors`, and `errors[].path`. |
| Which `onError` policy will clients use? | Server request options and request envelope. |
| Do generated client types match the UI plan? | Generated artifacts and client error policy. |
| Is the nullability change compatible for registered clients? | Schema registry and client registry validation. |

Use [Nitro](/docs/nitro/) when you need to verify what clients observe:

1. Open the schema and inspect `!`, list container nullability, and list item nullability in the SDL.
2. Run a focused operation in the [Operation pane](/docs/nitro/documents/operations/).
3. Inspect `data`, `errors`, and `errors[].path` in the [Response pane](/docs/nitro/documents/response/).
4. Compare `PROPAGATE` and `NULL` if your server allows request overrides.
5. Use the [schema registry](/docs/nitro/apis/schema-registry/) and [client registry](/docs/nitro/apis/client-registry/) when you need compatibility checks for known clients.

Nullability change direction matters:

| Change | Compatibility concern |
| --- | --- |
| Output nullable to non-null | Successful responses become stronger, but generated client types still change. Review clients that depended on `null`. |
| Output non-null to nullable | Clients must handle new absence, and generated types widen. |
| Input nullable to non-null | Breaking unless all existing operations already provide the value. |
| Input non-null to nullable or defaulted | Usually loosens the caller obligation, but review server semantics. |

For rollout planning, read [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) and [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

# Troubleshoot surprising nulls

Use symptoms to find the layer that caused the surprise before you change annotations.

| Symptom | Likely cause | What to inspect | Fix |
| --- | --- | --- | --- |
| A field is nullable in the schema even though the C# type looks non-null. | Nullable reference types are disabled, an attribute overrides inference, or descriptor configuration sets the type. | Generated SDL and C# project settings. | Enable nullable reference types or configure the field explicitly. |
| A non-null child error removes its parent. | Traditional `PROPAGATE` behavior moved `null` to the nearest nullable parent. | `errors[].path` and the parent field nullability. | Keep propagation, make the parent nullable on purpose, or evaluate `onError: "NULL"` with client support. |
| `onError: "NULL"` returns `null` at a non-null field position with an error. | The request or server default selected `NULL` behavior. | Request `onError`, server options, and field error path. | Add client throw-on-error or result-wrapper handling, or use `PROPAGATE`. |
| A client treats an errored `null` as business absence. | Client code checks only the value and ignores field errors. | Client response policy, generated types, and error handling. | Route field errors to UI boundaries, exceptions, or result wrappers. |
| A list has the expected container nullability but unexpected item behavior. | The item type differs from the container type, or an item error propagated through wrapping types. | SDL shape such as `[Review!]!` and `errors[].path` with list indexes. | Configure item nullability and test item-level failures. |
| Generated client code changed after a schema update. | Output or input nullability changed in SDL. | Schema diff and generated artifacts. | Treat the change as schema evolution and coordinate affected clients. |

A reliable diagnostic order is:

1. Inspect the generated schema.
2. Follow `errors[].path`.
3. Check whether the request used `onError`.
4. Check the server default and override policy.
5. Check whether the client has throw-on-error or result-wrapper handling.
6. Decide whether to change the schema contract, resolver behavior, server error mode, or client policy.

# Where to go next

Use this page as the conceptual hub, then go to the task-specific reference:

| If you need to | Go to |
| --- | --- |
| Configure non-null fields, C# mapping, attributes, or descriptors | [Non-Null](/docs/hotchocolate/v16/building-a-schema/non-null/) |
| Configure lists, nested lists, collection mappings, or list item nullability | [Lists](/docs/hotchocolate/v16/building-a-schema/lists/) |
| Understand resolver errors, partial data, error filters, and domain error modeling | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) and [Error handling](/docs/hotchocolate/v16/guides/error-handling/) |
| Review execution order and result completion | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) |
| Plan generated client types, fragments, response handling, and normalized caches | [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) |
| Roll out nullability changes safely | [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |
| Read the specification rules for types, lists, non-null, and field errors | [GraphQL type system](https://spec.graphql.org/October2021/#sec-Type-System), [wrapping types](https://spec.graphql.org/October2021/#sec-Wrapping-Types), and [field errors](https://spec.graphql.org/October2021/#sec-Handling-Field-Errors) |
| Follow the emerging semantic nullability direction | [GraphQL `onError` proposal](https://github.com/graphql/graphql-spec/pull/1163) and [GraphQL semantic nullability RFC](https://github.com/graphql/graphql-wg/blob/main/rfcs/SemanticNullability.md) |

Before you move on, pick one important field in your schema. Write down what `null` means, whether `!` matches the domain guarantee, what happens when the resolver errors, and how your clients handle the resulting response.
