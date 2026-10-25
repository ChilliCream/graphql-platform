---
title: "Versioning"
description: "Version a GraphQL schema in Hot Chocolate without URL versioning: deprecate fields with @deprecated and gate new features behind @requiresOptIn."
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

You can deprecate output fields, input fields, arguments, and enum values without any extra configuration. Object types can be deprecated too, but only once you enable a separate opt-in option (see [Deprecating Object Types](#deprecating-object-types)). Deprecated elements remain functional but are flagged in introspection, warning consumers to migrate.

<ExampleTabs>
<Implementation>

```csharp
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

The .NET `[Obsolete("reason")]` attribute works the same way as `[GraphQLDeprecated("reason")]` for fields, arguments, input fields, and enum values. Deprecating an object type itself is different: see [Deprecating Object Types](#deprecating-object-types).

</Implementation>
<Code>

```csharp
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

> [!WARNING]
> You cannot deprecate non-null arguments or input fields that have no default value. Deprecating a required field would silently break queries that depend on it.

## Deprecating Object Types

Deprecating an object type is not yet part of the released GraphQL specification. It tracks [graphql-spec RFC #997](https://github.com/graphql/graphql-spec/pull/997), which is still open, so its final shape could still change. Enable it deliberately, and treat it as subject to change until the RFC is merged.

Enable it in your schema options:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableObjectDeprecation = true);
```

Once enabled, `@deprecated` becomes valid on object types:

<ExampleTabs>
<Implementation>

```csharp
[GraphQLDeprecated("No longer known to exist.")]
public class Baiji
{
    public string? Name { get; set; }
}
```

</Implementation>
<Code>

```csharp
public class BaijiType : ObjectType<Baiji>
{
    protected override void Configure(IObjectTypeDescriptor<Baiji> descriptor)
    {
        descriptor.Deprecated("No longer known to exist.");
    }
}
```

</Code>
<Schema>

```graphql
type Baiji @deprecated(reason: "No longer known to exist.") {
  name: String
}
```

</Schema>
</ExampleTabs>

> [!NOTE]
> The .NET `[Obsolete]` attribute does not deprecate an object type; only `[GraphQLDeprecated]` on the class does. Every other deprecatable member honors both attributes identically, but honoring `[Obsolete]` on a class would silently deprecate types across existing codebases the moment this option was enabled, which is a schema-build failure for a field that returns such a type without itself being deprecated. Unifying the two attributes for object types is deferred to a future major version.

A field that is not itself deprecated cannot return a deprecated object type. Reaching a deprecated type indirectly is fine, so the following schema is valid even though `Baiji` is deprecated:

```graphql
type Query {
  animals: [Animal]
}

interface Animal {
  name: String
}

type Dog implements Animal {
  name: String
}

type Baiji implements Animal @deprecated(reason: "No longer known to exist.") {
  name: String
}
```

`animals` returns the interface `Animal`, not `Baiji` directly, so no field needs deprecating. Contrast a field that returns `Baiji` directly:

```graphql
type Query {
  baiji: Baiji
}

type Baiji @deprecated(reason: "No longer known to exist.") {
  name: String
}
```

> [!WARNING]
> `Query.baiji` is not deprecated but returns the deprecated `Baiji` type, so building the schema fails. Deprecate the field, or change its return type.

A deprecated object type remains a valid union member and interface implementation. Only the field returning it is checked.

Introspection exposes object type deprecation through `__Type.isDeprecated` and `__Type.deprecationReason`. Deprecated object types are hidden from `__schema.types` and `__Type.possibleTypes` by default:

```graphql
{
  __schema {
    types(includeDeprecated: true) {
      name
      isDeprecated
      deprecationReason
    }
  }
}
```

# Opt-In Features

While `@deprecated` marks schema elements that are going away, `@requiresOptIn` marks schema elements that are not yet stable. This is useful for rolling out experimental features, expensive operations, or anything where consumers should make a deliberate choice to use it.

Schema elements marked with `@requiresOptIn` are hidden from introspection by default. Consumers opt in by specifying the feature name.

## Enabling Opt-In Features

Opt-in feature support is disabled by default. Enable it in your schema options:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true);
```

## Marking Schema Elements as Opt-In

Apply `@requiresOptIn` to output fields, input fields, arguments, enum values, and directive definitions. The directive is repeatable, so a single element can require multiple features.

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

> [!WARNING]
> Like `@deprecated`, you cannot apply `@requiresOptIn` to non-null arguments or input fields without a default value. Hiding a required field would break queries.

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

The `includeOptIn` argument is available on `fields`, `args`, `inputFields`, `enumValues`, and `directives` in introspection queries. A directive definition exposes its own required features via `__Directive.requiresOptIn`, mirroring the `requiresOptIn` field on other introspection types.

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
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("experimentalInstantApi", "experimental");
```

</Implementation>
<Code>

```csharp
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

- **Need to add descriptions?** See [Documentation](./documentation.md).
- **Need to create custom directives?** See [Directives](./directives.md).
- **Need to understand schema evolution?** See [Extending Types](./object-types.md).
