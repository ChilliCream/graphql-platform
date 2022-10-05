---
title: Schema Options
---

Hot Chocolate distinguishes between schema and execution options. Schema options relate to the type system and execution options to the query engine.

| Member                 | Type     | Default        | Description                                                                 |
| ---------------------- | -------- | -------------- | --------------------------------------------------------------------------- |
| `QueryTypeName`        | `string` | `Query`        | The name of the query type.                                                 |
| `MutationTypeName`     | `string` | `Mutation`     | The name of the mutation type.                                              |
| `SubscriptionTypeName` | `string` | `Subscription` | The name of the subscription type.                                          |
| `StrictValidation`     | `bool`   | `true`         | Defines if the schema is allowed to have errors like missing resolvers etc. |

The schema options allow to alter the overall execution behaviour. The options can be set during schema creation.

```csharp
SchemaBuilder.New()
    .ModifyOptions(opt =>
    {
        opt.QueryTypeName = "Foo";
    })
    ...
    .Create()
```
