---
title: "Directives"
---

Directives add metadata to a GraphQL schema or alter the runtime execution of a query. Every GraphQL server provides the built-in directives `@skip`, `@include`, and `@deprecated`. Hot Chocolate lets you create custom directives with middleware that can transform field results, enforce authorization, or implement any cross-cutting concern.

There are two categories of directives:

- **Executable directives** appear in client queries and alter how the server processes specific fields or fragments.
- **Type-system directives** appear on schema definitions (types, fields, arguments) and provide metadata to clients and tooling.

# Built-in Directives

Hot Chocolate includes these directives out of the box:

| Directive                          | Location                       | Purpose                                       |
| ---------------------------------- | ------------------------------ | --------------------------------------------- |
| `@skip(if: Boolean!)`              | Fields, fragments              | Excludes a field when the condition is `true` |
| `@include(if: Boolean!)`           | Fields, fragments              | Includes a field when the condition is `true` |
| `@deprecated(reason: String)`      | Field definitions, enum values | Marks a schema element as deprecated          |
| `@requiresOptIn(feature: String!)` | Field definitions              | Marks a field as experimental (v16)           |

See [Versioning](/docs/hotchocolate/v16/defining-a-schema/versioning) for details on `@deprecated` and `@requiresOptIn`.

# Creating a Custom Directive

Create a class that inherits from `DirectiveType` and override the `Configure` method. Register it explicitly with `.AddDirectiveType<T>()`.

```csharp
// Types/MyDirectiveType.cs
public class MyDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddDirectiveType<MyDirectiveType>();
```

This registers a `@my` directive that can be applied to fields:

```graphql
query {
  product @my {
    name
  }
}
```

# Directive Arguments

Directives can accept arguments. Use a backing POCO class with `DirectiveType<T>` to define arguments as properties.

```csharp
// Types/CacheDirective.cs
public class CacheDirective
{
    public int MaxAge { get; set; }
}

// Types/CacheDirectiveType.cs
public class CacheDirectiveType : DirectiveType<CacheDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<CacheDirective> descriptor)
    {
        descriptor.Name("cache");
        descriptor.Location(DirectiveLocation.FieldDefinition);
    }
}
```

This produces `directive @cache(maxAge: Int!) on FIELD_DEFINITION`.

You can also define arguments without a POCO:

```csharp
// Types/CacheDirectiveType.cs
public class CacheDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("cache");
        descriptor.Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument("maxAge")
            .Type<NonNullType<IntType>>();
    }
}
```

# Repeatable Directives

By default, a directive can appear only once at a given location. To allow a directive to be applied multiple times, mark it as repeatable.

```csharp
// Types/TagDirectiveType.cs
public class TagDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("tag");
        descriptor.Location(DirectiveLocation.FieldDefinition);
        descriptor.Repeatable();
    }
}
```

This produces `directive @tag repeatable on FIELD_DEFINITION`.

# Directive Locations

A directive declares where it can be applied. Combine multiple locations with the pipe operator.

```csharp
descriptor.Location(DirectiveLocation.Field | DirectiveLocation.Object);
```

## Type-System Locations

Type-system directives appear on schema definitions. Their argument values are fixed at schema build time and are visible through introspection.

Common locations include `OBJECT`, `FIELD_DEFINITION`, `INPUT_OBJECT`, `INPUT_FIELD_DEFINITION`, `INTERFACE`, `ENUM`, `ENUM_VALUE`, `UNION`, `SCALAR`, and `ARGUMENT_DEFINITION`.

## Executable Locations

Executable directives appear in client queries. Locations include `FIELD`, `FRAGMENT_SPREAD`, `INLINE_FRAGMENT`, `QUERY`, `MUTATION`, and `SUBSCRIPTION`.

# Applying Directives to Types

You can attach a type-system directive to a type definition.

```csharp
// Types/ProductType.cs
public class ProductType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Product");
        descriptor.Directive(new CacheDirective { MaxAge = 60 });
    }
}
```

Using the POCO form is type-safe. You can also use the string-based form, but it is more error-prone:

```csharp
descriptor.Directive("cache", new ArgumentNode("maxAge", 60));
```

# Directive Middleware

Directives become powerful when you attach middleware. A directive middleware can modify field results, short-circuit resolution, or add side effects.

```csharp
// Types/UpperCaseDirectiveType.cs
public class UpperCaseDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("upperCase");
        descriptor.Location(DirectiveLocation.FieldDefinition);

        descriptor.Use((next, directive) => async context =>
        {
            await next.Invoke(context);

            if (context.Result is string s)
            {
                context.Result = s.ToUpperInvariant();
            }
        });
    }
}
```

Middleware runs in the order directives appear. For a field with `@a @b @c`, the middleware executes in order: `a`, then `b`, then `c`. Directives on the object type run before directives on the field definition, which run before directives in the query.

Each middleware can call `next.Invoke(context)` to pass execution to the next directive. Skipping the `next` call short-circuits the pipeline.

# Directive Execution Order

Given this schema and query:

```graphql
type Bar @a @b {
  baz: String @c @d
}
```

```graphql
{
  foo {
    baz @e @f
  }
}
```

The directive middleware executes in order: `a`, `b`, `c`, `d`, `e`, `f`. Object-level directives run first, followed by field-definition directives, followed by query directives.

# Troubleshooting

## Directive not recognized in query

Verify the directive is registered with `.AddDirectiveType<T>()` and that its location includes executable locations (like `FIELD`). Type-system directives cannot be used in client queries.

## Middleware not executing

Confirm the directive location matches where it is applied. A directive with `DirectiveLocation.Object` does not fire when applied to a field in a query.

## Duplicate directive error

If a non-repeatable directive is applied twice at the same location, validation fails. Either mark the directive as `.Repeatable()` or apply it only once.

# Next Steps

- **Need to deprecate fields?** See [Versioning](/docs/hotchocolate/v16/defining-a-schema/versioning).
- **Need to authorize fields?** See [Authorization](/docs/hotchocolate/v16/security/authorization).
- **Need to extend types?** See [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types).
