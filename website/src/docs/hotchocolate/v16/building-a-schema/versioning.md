---
title: "Versioning"
---

Unlike REST APIs, GraphQL schemas do not use URL-based versioning (like `/graphql/v2`). Most schema changes are additive and non-breaking: adding new types and new fields does not affect existing queries. Removing a field or changing its nullability, however, is a breaking change.

GraphQL provides two directives to manage the lifecycle of schema elements:

- `@deprecated` signals that a field is being phased out and consumers should migrate away.
- `@requiresOptIn` signals that a field is not yet stable and requires explicit consumer consent.

```graphql
type Query {
  users: [User] @deprecated(reason: "Use the `authors` field instead")
  authors: [User]
  recommendations: [Book] @requiresOptIn(feature: "experimentalRecommendations")
}
```

# Deprecation

You can deprecate output fields, input fields, arguments, and enum values. Deprecated elements remain functional but are flagged in introspection, warning consumers to migrate.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    [GraphQLDeprecated("Use the `authors` field instead")]
    public static User[] GetUsers()
    {
        // ...
    }

    public static User[] GetAuthors()
    {
        // ...
    }
}
```

The .NET `[Obsolete("reason")]` attribute works the same way as `[GraphQLDeprecated("reason")]`.

</Implementation>
<Code>

```csharp
// Types/BookQueriesType.cs
public class BookQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Deprecated("Use the `authors` field instead")
            .Resolve(context =>
            {
                // ...
            });
    }
}
```

</Code>
</ExampleTabs>

> Warning: You cannot deprecate non-null arguments or input fields that have no default value. Deprecating a required field would silently break queries that depend on it.

# Opt-In Features

While `@deprecated` marks fields that are going away, `@requiresOptIn` marks fields that are not yet stable. This is useful for rolling out experimental features, expensive operations, or anything where consumers should make a deliberate choice to use it.

Fields marked with `@requiresOptIn` are hidden from introspection by default. Consumers opt in by specifying the feature name.

## Enabling Opt-In Features

Opt-in feature support is disabled by default. Enable it in your schema options:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

## Marking Fields as Opt-In

Apply `@requiresOptIn` to output fields, input fields, arguments, and enum values. The directive is repeatable, so a single field can require multiple features.

<ExampleTabs>
<Implementation>

```csharp
// Types/Session.cs
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
// Types/SessionType.cs
public class SessionType : ObjectType<Session>
{
    protected override void Configure(IObjectTypeDescriptor<Session> descriptor)
    {
        descriptor
            .Field(f => f.StartInstant)
            .RequiresOptIn("experimentalInstantApi");

        descriptor
            .Field(f => f.EndInstant)
            .RequiresOptIn("experimentalInstantApi");
    }
}
```

</Code>
</ExampleTabs>

> Warning: Like `@deprecated`, you cannot apply `@requiresOptIn` to non-null arguments or input fields without a default value. Hiding a required field would break queries.

## Introspection

Consumers discover opt-in fields by passing the `includeOptIn` argument:

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

The `includeOptIn` argument is available on `fields`, `args`, `inputFields`, and `enumValues` in introspection queries.

To discover all opt-in features in the schema:

```graphql
{
  __schema {
    optInFeatures
  }
}
```

## Feature Stability

You can declare the stability level of each opt-in feature. This helps consumers understand whether a feature is experimental, preview, or has some other status.

<ExampleTabs>
<Implementation>

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalInstantApi", "experimental");
```

</Implementation>
<Code>

```csharp
// Program.cs
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .SetSchema(s => s
        .OptInFeatureStability("experimentalInstantApi", "experimental"));
```

</Code>
</ExampleTabs>

Consumers query feature stability through introspection:

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

# Next Steps

- **Need to add descriptions?** See [Documentation](/docs/hotchocolate/v16/defining-a-schema/documentation).
- **Need to create custom directives?** See [Directives](/docs/hotchocolate/v16/defining-a-schema/directives).
- **Need to understand schema evolution?** See [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types).
