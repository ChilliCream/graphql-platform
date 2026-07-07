---
date: "2026-07-07"
title: "Directives All the Way Down"
description: "GraphQL directives can now be applied to directive definitions themselves in Hot Chocolate 16.4, so you can finally deprecate a directive and attach metadata to your schema's own extension points."
tags: ["hotchocolate", "graphql", "directives", "deprecation"]
featuredImage: "header.png"
author: Glen
authorUrl: https://github.com/glen-84
authorImageUrl: https://avatars.githubusercontent.com/u/261509?v=4
---

Directives are GraphQL's built-in extension mechanism. Every time you write `@deprecated`, `@skip`, `@include`, or a custom directive of your own, you attach a small piece of behavior or metadata to a specific part of your schema or query. Over the years you have been able to apply them almost everywhere: on fields, arguments, object types, enum values, input fields, scalars, and more.

Almost everywhere. There was one conspicuous gap. You could never apply a directive to a _directive definition_ itself.

That sounds like a technicality until you hit it, and the most common way to hit it is this: you have a custom directive you would like to retire, so you reach for `@deprecated`, only to find it is not allowed there. There was no way, in the schema itself, to signal that a directive was on its way out.

That gap is now closed. As of Hot Chocolate 16.4, directives can be applied to directive definitions, and `@deprecated` is one of them.

# A long time coming

This is not a Hot Chocolate invention. It is a GraphQL specification feature, and it took a while to get there. The idea goes back years, with earlier attempts like [#567](https://github.com/graphql/graphql-spec/issues/567) and [#907](https://github.com/graphql/graphql-spec/pull/907) exploring how directives on directives should work. It finally came together in [graphql-spec #1206](https://github.com/graphql/graphql-spec/pull/1206), which was accepted into the specification on June 4, 2026, after moving through the RFC process and landing alongside a reference implementation in graphql-js. Hot Chocolate 16.4 ships with full support.

# What it looks like

Before a directive can be applied to a directive definition, it has to opt in by declaring the new `DIRECTIVE_DEFINITION` location. In schema-first SDL that is just another entry in the `on` list:

```graphql
directive @onDirectiveDefinition on DIRECTIVE_DEFINITION
```

In C#, you can declare that location the implementation-first way, with an attribute:

```csharp
[DirectiveType(DirectiveLocation.DirectiveDefinition)]
public class OnDirectiveDefinition
{
}
```

or the code-first way, on the descriptor:

```csharp
public class OnDirectiveDefinitionType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("onDirectiveDefinition");
        descriptor.Location(DirectiveLocation.DirectiveDefinition);
    }
}
```

Once a directive targets that location, you can apply it to another directive definition. In SDL the applied directives go after the arguments and before the `on` keyword:

<!-- prettier-ignore -->
```graphql
directive @custom(name: String!) @onDirectiveDefinition on OBJECT
```

In code-first, call `Directive<T>()` on the descriptor with the directive you declared, which keeps the reference type-safe:

```csharp
public class CustomDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("custom");
        descriptor.Location(DirectiveLocation.Object);
        descriptor.Argument("name").Type<NonNullType<StringType>>();
        descriptor.Directive<OnDirectiveDefinition>();
    }
}
```

# Finally, deprecating a directive

This is the one everybody was waiting for. `@deprecated` has always been able to mark fields, arguments, input fields, and enum values as obsolete. Now it can mark a directive definition too.

Say you shipped a custom `@legacyAuth` directive and you are moving everyone over to a new `@authorize`. Until now you had no in-schema way to say "stop using this." You would drop a note in a changelog and hope people read it. Now the schema says it for you:

<!-- prettier-ignore -->
```graphql
directive @legacyAuth @deprecated(reason: "Use @authorize instead.") on FIELD_DEFINITION
```

In code-first, call `Deprecated(...)` on the descriptor:

```csharp
public class LegacyAuthDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("legacyAuth");
        descriptor.Location(DirectiveLocation.FieldDefinition);
        descriptor.Deprecated("Use @authorize instead.");
    }
}
```

Or, on an annotated directive class, reach for the attributes you already know. Both `[Obsolete(...)]` and `[GraphQLDeprecated(...)]` set the deprecation:

```csharp
[Obsolete("Use @authorize instead.")]
[DirectiveType(DirectiveLocation.FieldDefinition)]
public class LegacyAuth
{
}
```

You can also deprecate a single argument instead of the whole directive, which is handy when you are evolving a directive's signature rather than replacing it outright:

<!-- prettier-ignore -->
```graphql
directive @custom(
  legacyArg: Int @deprecated(reason: "Use newArg instead.")
  newArg: String
) on OBJECT
```

Because this is real schema metadata, it shows up in introspection, exactly the way deprecated fields and enum values always have. `__Directive` now carries `isDeprecated` and `deprecationReason`, and `__Schema.directives` gained an `includeDeprecated` argument that hides deprecated directives by default:

```graphql
{
  __schema {
    directives(includeDeprecated: true) {
      name
      isDeprecated
      deprecationReason
    }
  }
}
```

That means tooling gets it for free. IDEs, code generators, and Nitro can render a strikethrough or a warning on a deprecated directive the same way they already do for a deprecated field, so the deprecation actually reaches the people using it.

# Beyond deprecation

Deprecation is the headline, but it is not the whole story. A directive definition is now just another annotatable schema element, and Hot Chocolate already uses that for more than `@deprecated`.

`@requiresOptIn` can be applied to a directive definition to mark it as experimental. Consumers have to opt in to the named feature before they rely on the directive, so you can ship something new without committing to it right away:

<!-- prettier-ignore -->
```graphql
directive @experimentalTrace @requiresOptIn(feature: "experimentalTracing") on FIELD_DEFINITION
```

`@tag` applies to directive definitions too, letting you group and filter directives with the same labels you already apply to types and fields.

And because this is an open extension point, your own directives are welcome too. Say you want each directive to point at the design doc that introduced it, the way `@specifiedBy` links a scalar to its specification:

<!-- prettier-ignore -->
```graphql
directive @designDoc(url: String!) on DIRECTIVE_DEFINITION

directive @authorize @designDoc(url: "https://example.com/rfcs/authz") on FIELD_DEFINITION
```

You can also fold an annotation into an existing definition with `extend directive`, without touching the original declaration:

<!-- prettier-ignore -->
```graphql
extend directive @custom @deprecated(reason: "Use @modern instead.")
```

One rule is specific to this feature: a directive cannot be applied to its own definition. Hot Chocolate rejects that self-reference with a clear error.

# What will you build?

We shipped the mechanism, and Hot Chocolate already puts it to work in more than one way: `@deprecated`, `@requiresOptIn`, and `@tag` all apply to directive definitions today. The interesting part is what you do next.

Directives are an open-ended extension point, and now that they reach directives themselves, we are genuinely curious what patterns you will come up with. If you build something interesting on top of this, tell us. And if you just want to deprecate that one directive that has been haunting your schema, that is a perfectly good reason to upgrade too.

For the full reference, see the [Directives on Directive Definitions](../docs/hotchocolate/defining-a-schema/directives.md#directives-on-directive-definitions) guide.
