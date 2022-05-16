---
title: "Subscriptions"
---

A Subscription type cannot be stitched from downstream services so it must be defined directly in the gateway schema.

> [Learn more about defining a Subscription type](/docs/hotchocolate/defining-a-schema/subscriptions)

> ⚠️ Note: Subscription stitching is coming in v13

After adding a Subscription type to the gateway service, you may encounter an error when building the gateway schema.

```csharp
1. The schema builder was unable to identify the query type of the schema. Either specify which type is the query type or set the schema builder to non-strict validation mode.
```

If you turn off strict validation and generate the schema, the `schema` element won't include a `query` field despite a Query type being defined.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(o =>
        {
            o.StrictValidation = false;
        }
    )
```

```json
schema {
  subscription: Subscription
}

type Query {
  messages: [Message!]!
}
```

To resolve this issue, use the Schema Options to specify the `QueryTypeName` and `MutationTypeName`.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(o =>
        {
            o.QueryTypeName = "Query";
            o.MutationTypeName = "Mutation";
        }
    )
```

Generating the schema again results in a valid schema.

```json
schema {
  query: Query
  subscription: Subscription
}

type Query {
  messages: [Message!]!
}

type Subscription {
  onMessagePosted: Message!
}
```