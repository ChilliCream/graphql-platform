---
date: "2026-09-08"
title: "Opt-In Features: The Other Half of @deprecated"
description: "The @requiresOptIn directive gives GraphQL a standard way to ship a field before it is stable. Here is where it came from, how it pairs with @deprecated, and how to use it in Hot Chocolate and Fusion."
tags:
  [
    "hotchocolate",
    "fusion",
    "graphql",
    "directives",
    "opt-in-features",
    "schema-evolution",
  ]
featuredImage: "header.png"
author: Glen
authorUrl: https://github.com/glen-84
authorImageUrl: https://avatars.githubusercontent.com/u/261509?v=4
---

Every schema has one of these. A field that works, that a couple of teams are already excited about, and that you are not quite ready to promise. Maybe its shape is still moving. Maybe it is expensive and you want to know who is calling it. Maybe you just want feedback before you commit to supporting it for years.

GraphQL has always had a good answer for the _end_ of a field's life. `@deprecated` says "this is going away", tooling hides it, and clients get a warning when they use it anyway. For the _beginning_ of a field's life there was nothing. A new field was either in the schema and fully public, or it was not there at all.

`@requiresOptIn` fills that gap. It marks a schema element as available but not yet stable, hides it from introspection by default, and lets clients opt in to a named feature when they are ready to take it on. Hot Chocolate 16 supports it, and as of Fusion 16.4, so does the gateway.

# Where it came from

The idea is borrowed from Kotlin. Kotlin library authors mark an unstable API with [`@RequiresOptIn`](https://kotlinlang.org/docs/opt-in-requirements.html), and callers have to acknowledge that with `@OptIn` before the compiler lets them use it. The API is there, it is documented, and nobody stumbles into it by accident.

In May 2022, Martin Bonnin of the Apollo Kotlin team proposed the same contract for GraphQL schemas in [graphql-spec #943](https://github.com/graphql/graphql-spec/issues/943), with the full text in the working group's [Opt-in Features RFC](https://github.com/graphql/graphql-wg/blob/main/rfcs/OptInFeatures.md). The directive was first called `@experimental`, then `@optIn`, and finally `@requiresOptIn`, which says more precisely what it does and leaves `@optIn` free for the client side. The RFC defines it like this:

<!-- prettier-ignore -->
```graphql
directive @requiresOptIn(feature: String!) repeatable
  on FIELD_DEFINITION | ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION | ENUM_VALUE | DIRECTIVE_DEFINITION
```

The proposal is still at Stage 0 (Strawman) in the GraphQL RFC process, and its introspection design is waiting on the broader question of how directive metadata should surface through introspection. That has not stopped it from being useful. Apollo Kotlin already understands `@requiresOptIn` on the client and turns it into Kotlin opt-in annotations on the generated code. Hot Chocolate implements the server side, the same way it does for `@defer` and `@stream`: when a draft feature solves a real problem, we ship it behind an option and feed what we learn back into the RFC.

# The other half of `@deprecated`

`@deprecated` and `@requiresOptIn` are two ends of the same lifecycle. Together they turn "it is in the schema or it is not" into four stages: experimental, stable, deprecated, and removed.

|                       | `@deprecated`             | `@requiresOptIn`              |
| --------------------- | ------------------------- | ----------------------------- |
| Signals               | The element is going away | The element is not yet stable |
| Introspection default | Hidden                    | Hidden                        |
| Revealed with         | `includeDeprecated: true` | `includeOptIn: ["feature"]`   |
| Carries               | A reason                  | One or more feature names     |
| At execution time     | Still resolves            | Still resolves                |

The two directives mirror each other closely, with one important difference. A deprecation is a fact about a single element, but an opt-in feature is a _name_. The same feature name can span a field on one type, an enum value on another, and an argument somewhere else, and a client opts in to the whole feature at once. That is what makes a rollout coherent: when you promote the feature, you promote every piece of it together.

Two things `@requiresOptIn` is not. It is not authorization. A hidden field still resolves for anyone who knows its name, so if access has to be restricted, use authorization. And it is not a deprecation in disguise. A deprecated field asks clients to stop using it. An opt-in field invites them to start, with their eyes open.

# Using it in Hot Chocolate

Opt-in features are off by default. Enable them in the schema options:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

Then mark a schema element. Say a shipping API has a delivery estimate that is still being tuned. Implementation-first, that is an attribute:

```csharp
public class Shipment
{
    public int Id { get; set; }

    public required string TrackingNumber { get; set; }

    [RequiresOptIn("deliveryEstimates")]
    public DateTime? EstimatedDelivery { get; set; }
}
```

Code-first, it is a call on the descriptor:

```csharp
public class ShipmentType : ObjectType<Shipment>
{
    protected override void Configure(IObjectTypeDescriptor<Shipment> descriptor)
    {
        descriptor
            .Field(f => f.EstimatedDelivery)
            .RequiresOptIn("deliveryEstimates");
    }
}
```

And schema-first, it is the directive itself:

```graphql
type Shipment {
  id: Int!
  trackingNumber: String!
  estimatedDelivery: DateTime @requiresOptIn(feature: "deliveryEstimates")
}
```

Fields are the common case, but the directive works on arguments, input fields, enum values, and, since Hot Chocolate 16.4, directive definitions too. An enum value is a nice fit for a capability you are trialing:

```graphql
enum ShippingSpeed {
  STANDARD
  EXPRESS
  DRONE @requiresOptIn(feature: "droneDelivery")
}
```

The directive is repeatable, so an element can belong to more than one feature. A client that opts in to any one of them sees it.

There is one rule to keep in mind. A required input has to be visible to every client, so a non-null argument or input field without a default value cannot be marked `@requiresOptIn`. Schema validation rejects it.

## Seeing what is hidden

Everything marked `@requiresOptIn` stays out of introspection until the client asks for it. The `includeOptIn` argument on `fields`, `args`, `inputFields`, `enumValues`, and `directives` takes the list of feature names the client has opted in to, and each element reports its own requirements through `requiresOptIn`:

```graphql
{
  __type(name: "Shipment") {
    fields(includeOptIn: ["deliveryEstimates"]) {
      name
      requiresOptIn
    }
  }
}
```

The schema also lists every feature it offers, so tooling can show a client what it is missing:

```graphql
{
  __schema {
    optInFeatures
  }
}
```

## Saying how stable a feature is

"Not yet stable" covers a lot of ground, from "we wrote this last week" to "this ships next quarter". Hot Chocolate extends the proposal with a stability level per feature. Declare it on the executor builder:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("deliveryEstimates", "experimental");
```

Or, schema-first, with the `@optInFeatureStability` directive on the schema definition:

```graphql
schema
  @optInFeatureStability(
    feature: "deliveryEstimates"
    stability: "experimental"
  ) {
  query: Query
}
```

The stability value is a free-form string, so you can use whatever vocabulary your organization already has: `experimental`, `preview`, `beta`, or anything else. Clients read it back through introspection:

```graphql
{
  __schema {
    optInFeatureStability {
      feature
      stability
    }
  }
}
```

# Using it in Fusion

In a federated setup, the interesting question is what happens when the teams behind a field disagree about whether it is stable. Fusion 16.4 answers it with a simple rule: opt-in is contagious.

Each subgraph enables opt-in features and marks its own source schema, exactly as above:

```csharp
builder
    .AddGraphQL("Shipping")
    .AddTypes()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("deliveryEstimates", "experimental");
```

The gateway enables them too, which is what turns on `includeOptIn` and the `optInFeatures` and `optInFeatureStability` fields in the gateway's introspection:

```csharp
builder
    .AddGraphQLGateway()
    .AddFileSystemConfiguration("./gateway.far")
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

Composition merges `@requiresOptIn` by union. If a shareable field carries the directive in at least one source schema, it carries it in the execution schema, and the feature names from every source schema are kept. That is the inverse of how `@deprecated` composes: one source schema can deprecate a shared field for everyone, and one source schema can gate a shared field for everyone, but only all of them together can make it generally available again. A field is stable when every team that owns it says so.

Stability levels have to agree. If two source schemas declare different stability values for the same feature, composition fails with `OPT_IN_FEATURE_STABILITY_MISMATCH` until the declarations agree.

At runtime the gateway behaves like a single Hot Chocolate server. Opt-in members are hidden from introspection until the client passes `includeOptIn`, and a request that selects one of them executes regardless.

# From experimental to stable

Promoting a feature is the easy part. Remove the `@requiresOptIn` directives and the stability declaration, and the elements appear in introspection for everyone. Clients that opted in early keep working unchanged. In Fusion, remove it from every source schema that defines the field.

From there, the field lives its normal life. If it ever has to go, `@deprecated` is waiting at the other end.

# Try it

Opt-in features are in Hot Chocolate 16 and Fusion 16.4 today. The [Versioning](../docs/hotchocolate/defining-a-schema/versioning.md#opt-in-features) guide covers the Hot Chocolate side in detail, and [Schema Exposure and Evolution](../docs/fusion/schema-exposure-and-evolution.md#experimental-and-preview-features) covers composition and the gateway.

The RFC itself is still open, and real-world usage is what moves a proposal forward. If you ship something behind `@requiresOptIn`, we would love to hear how it went, and so would the [working group](https://github.com/graphql/graphql-spec/issues/943).
