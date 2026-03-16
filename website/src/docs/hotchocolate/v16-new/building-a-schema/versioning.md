---
title: "Versioning"
---

Whilst we could version our GraphQL API similar to REST, i.e. `/graphql/v1`, it is not a good practice and leads to ever increasing maintenance overhead in our complete pipeline.

Many changes to a GraphQL schema are non-breaking. We can freely add new types and extend existing types with new fields. This does not break existing queries.
However removing a field or changing its nullability does.

GraphQL provides two directives to manage the lifecycle of schema elements. Fields that are not yet stable can be marked with `@requiresOptIn`, requiring consumers to explicitly opt in before using them. Fields that are being phased out can be marked with `@deprecated`, signaling that consumers should migrate away before the field is removed.

```sdl
type Query {
  users: [User] @deprecated(reason: "Use the `authors` field instead")
  authors: [User]
  recommendations: [Book] @requiresOptIn(feature: "experimentalRecommendations")
}
```

# Deprecation

You can deprecate output fields, input fields, arguments and enum values.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    [GraphQLDeprecated("Use the `authors` field instead")]
    public User[] GetUsers()
    {
        // Omitted code for brevity
    }
}
```

> Note: .NET's `[Obsolete("reason")]` attribute is handled in the same way as `[GraphQLDeprecated("reason")]`.

</Implementation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("users")
            .Deprecated("Use the `authors` field instead")
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          users: [User] @deprecated(""Use the `authors` field instead"")
        }
    ");
```

</Schema>
</ExampleTabs>

> Warning: You can not deprecate non-null arguments or input fields without a default value.

# Opt-In Features

While `@deprecated` signals that a field is going away, `@requiresOptIn` is its counterpart: it signals that a field is not yet stable and requires explicit consent before being used. This is useful for rolling out experimental features, expensive operations, or anything where the consumer should make a conscious decision to use it.

Fields marked with `@requiresOptIn` are hidden from introspection by default. Consumers must explicitly opt in to see and use them.

```sdl
type Session {
  id: ID!
  title: String!
  startInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
  endInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
}
```

## Enabling Opt-In Features

The opt-in features support is disabled by default. You need to enable it in your schema options.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

## Marking Fields as Opt-In

You can apply `@requiresOptIn` to output fields, input fields, arguments and enum values. The directive is repeatable, so a single field can require multiple features.

<ExampleTabs>
<Implementation>

```csharp
public class Session
{
    public string Id { get; set; }

    public string Title { get; set; }

    [RequiresOptIn("experimentalInstantApi")]
    public Instant? StartInstant { get; set; }

    [RequiresOptIn("experimentalInstantApi")]
    public Instant? EndInstant { get; set; }
}
```

</Implementation>
<Code>

```csharp
public class SessionType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Session");

        descriptor
            .Field("startInstant")
            .Type<InstantType>()
            .RequiresOptIn("experimentalInstantApi")
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Code>
<Schema>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Session {
          id: ID!
          title: String!
          startInstant: Instant
            @requiresOptIn(feature: ""experimentalInstantApi"")
          endInstant: Instant
            @requiresOptIn(feature: ""experimentalInstantApi"")
        }
    ");
```

</Schema>
</ExampleTabs>

> Warning: Like `@deprecated`, you can not apply `@requiresOptIn` to non-null arguments or input fields without a default value, since hiding a required field would break queries.

## Introspection

Fields marked with `@requiresOptIn` are hidden from introspection by default. To discover them, consumers pass the `includeOptIn` argument with the feature names they want to see.

```graphql
{
  __type(name: "Session") {
    fields(includeOptIn: ["experimentalInstantApi"]) {
      name
      requiresOptIn
    }
  }
}
```

The `includeOptIn` argument is available on `fields`, `args`, `inputFields` and `enumValues` in introspection queries.

To discover which opt-in features exist in the schema:

```graphql
{
  __schema {
    optInFeatures
  }
}
```

## Feature Stability

You can declare the stability level of each opt-in feature at the schema level using `@optInFeatureStability`. This lets consumers know whether a feature is experimental, preview, or any other stability level you define.

<ExampleTabs>
<Code>

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalInstantApi", "experimental");
```

Alternatively, you can set it in the schema configuration:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .SetSchema(s => s
        .OptInFeatureStability("experimentalInstantApi", "experimental"));
```

</Code>
<Schema>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        schema
          @optInFeatureStability(
            feature: ""experimentalInstantApi""
            stability: ""experimental"") {
          query: Query
        }
    ");
```

</Schema>
</ExampleTabs>

Consumers can query feature stability through introspection:

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
