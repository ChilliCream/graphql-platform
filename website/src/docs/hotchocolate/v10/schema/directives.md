---
title: Directives
---

# Introduction

Directives provide a way in GraphQL to add annotations to the type system or query elements. These annotations can be used to provide meta data for code generators or even to change the execution behavior of the query engine on a GraphQL server.

You can specify a directive by inheriting from `DirectiveType`:

```csharp
public class MyDirective
    : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);
    }
}
```

In order to use a directive it has to be registered with the schema.

```csharp
SchemaBuilder.New()
    .AddDirectiveType<MyDirective>()
    .Create();
```

GraphQL specifies three directives in the spec (skip, include and deprecated) which are always registered with your schema.

The skip and include directives can be used in queries to in- or exclude fields from your query.

```graphql
query foo($hideField: Boolean = false) {
  hello @skip(if: $hideField)
}
```

# Repeatable

Directives are by default unique, that means that you can annotate a directive just once to an object. If you want to add a specific directive type multiple times you have to specify the directive as repeatable.

```csharp
public class MyDirective
    : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Field);
        descriptor.Repeatable();
    }
}
```

# Typed Arguments

Directive can have arguments that can be used to make them more flexible. So, if we had a directive like the following:

```csharp
public class MyDirective
    : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Argument("name").Type<NonNullType<StringType>>();
        descriptor.Location(DirectiveLocation.Object);
    }
}
```

We could associate the `MyDirective` with an object like this:

```csharp
public class FooType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Directive("my", new StringValue("bar"));
        ...
    }
}
```

Adding directives just with their name is not type safe and could lead to runtime errors which can be avoided by using our generic variant of the directive type. The generic directive type declares the .NET type that represents the directive instance.

```csharp
public class MyDirectiveType
    : DirectiveType<MyDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        ...
    }
}

public class MyDirective
{
    public string Name { get; set; }
}
```

The generic directive type works similar to the generic input object type. The directive descriptor will automatically try to discover any properties and expose those as arguments.

So, with our new directive in place we could now add it to our object type like the following:

```csharp
public class FooType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Directive(new MyDirective { Name = "bar" });
        ...
    }
}
```

Since, the directive instance that we have added to our type is now a strong .NET type we do not have to fear changes to the directive structure or its name anymore.

# Middleware

What makes directive with Hot Chocolate very useful is the ability to associate a middleware with it. A middleware can alternate the result or even produce the result of a field. A directive middleware is only added to a field middleware pipeline when the directive was annotated to the object definition, the field definition or the field.

Moreover, of the directive is repeatable the middleware will be added multiple times to the middleware allowing to build a real pipeline with it.

In order to add a middleware to a directive you could declare it with the descriptor as a delegate.

```csharp
public class MyDirectiveType
    : DirectiveType<MyDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("my");
        descriptor.Location(DirectiveLocation.Object);
        descriptor.Use(next => context =>
        {
            context.Result = "Bar";
            return next.Invoke(context);
        })
    }
}

public class MyDirective
{
    public string Name { get; set; }
}
```

Directives with middleware or executable directives can be put on object types and on their field definitions or on the field selection in a query. Executable directives on an object type will replace the field resolver of every field of the annotated object type.

## Order

In GraphQL the directive order is significant and with our middleware we use the order of directives to create a resolver pipeline through which the result flows.

The resolver pipeline consists of a sequence of directive delegates, called one after the other.

Each delegate can perform operations before and after the next delegate. A delegate can also decide to not pass a resolver request to the next delegate, which is called short-circuiting the resolver pipeline. Short-circuiting is often desirable because it avoids unnecessary work.

The order of middleware pipeline is defined by the order of the directives. Since, executable directives will flow from the object type to its field definitions the directives of the type would be called first in the order that they were annotated.

```sdl
type Query {
  foo: Bar
}

type Bar @a @b {
  baz: String @c @d
}
```

So, the directives in the above example would be called in the following order `a, b, c, d`.

If there were more directives in the query, they would be appended to the directives from the type.

```graphql
{
  foo {
    baz @e @f
  }
}
```

So, now the order would be like the following: `a, b, c, d, e, f`.

Every middleware can execute the original resolver function by calling `ResolveAsync()` on the `IDirectiveContext`.
