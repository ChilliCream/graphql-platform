---
path: "/blog/2019/11/29/schema-design"
date: "2019-11-29"
title: "Lets supercharge your GraphQL schema :)"
featuredImage: "shared/hotchocolate-banner.png"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

When you think about how we build our GraphQL schemas with Hot Chocolate we always need to fall back to either the schema types or the GraphQL SDL in order to get the typings right.

This brings with it a lot of boiler plate code that we actually could infer from our C# code. With version 10.3.0 we have decided to integrate some of the version 11 features to make it possible to have these capabilities now instead of next year.

## Nullability

First, let us get the obvious out of the way. C# with version 8.0 has now nullable reference types, or actually they now have non-null reference types.

It does not matter how you look at it, but the result is that we now can differentiate between nullable reference types and reference types that cannot be null.

With version 10.3.0-preview.2 we can infer these, and you finally do not need helpers like attributes and other things to define your schema types with non-null types.

A simple query like ...

```csharp
public class Query
{
    /// <summary>
    /// This field says hello.
    /// </summary>
    public string SayHello(string? name)
    {
        return name is null ? "Hello!" : $"Hello {name}!"
    }
}
```

... is now correctly inferred to:

```graphql
type Query {
  "This field says hello."
  sayHello(name: String): String!
}
```

Do not get me wrong here, I still love our schema types and we will not get rid of them since they are the foundation of every schema. We still are using them in the above example, you just do not need to see them anymore. Moreover, we see these improvements more as an additional way to define a GraphQL schemas with pure C# types.

In the beginning we decided that people should be free in their way of how they want to define their schemas. We are still 100% committed to SDL first, code-first with schema types and code-first with pure C# types.

## Interfaces

Since version 10.0.0 Hot Chocolate is able to infer interface types from API usage. This means that we will correctly infer the interfaces that you use and the types that implement those interfaces.

```csharp
public class Query
{
    /// <summary>
    /// Get my pet :)
    /// </summary>
    public IPet? GetPet(int id)
    {
        // some code
    }
}

public interface IPet
{
    // some code
}

public class Dog : IPet
{
    // some code
}

public class Cat : IPet
{
    // some code
}

SchemaBuilder.New()
    .AddQuery<Query>()
    .AddType<Dog>()
    .AddType<Cat>()
    .Create();
```

```graphql
type Query {
  "Get my pet :)"
  pet(id: Int!): IPet
}

interface Pet {
    // fields
}

type Dog implements Pet {
    // fields
}

type Cat implements Pet {
    // fields
}
```

This feels awesome. The schema builder translates our C# types exactly the way we meant them. We do not have to tell the schema builder any more how to do that it will just work.

## Descriptor Attributes

But what about field middleware and other more complex features like type extensions and so on.

This was something we contemplated for a long time. In the end we came up with powerful descriptor attributes. This basically allows you to create attributes for your schema in which you have access to the full descriptor API. Let me give you an example for this.

Let us say we want to create a simple middleware that can be put on properties and methods and that applies a `ToUpper` on every resulting `string` on the annotated member.

```csharp
public sealed class ToUpperAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(IObjectFieldDescriptor descriptor)
    {
        descriptor.Use(next => async ctx =>
        {
            await next(ctx);

            if(ctx.Result is string s)
            {
                ctx.Result = s.ToUpperInvariant();
            }
        })
    }
}
```

The neat thing is that we have full access to all the things we have on our fluent API. The attributes very cleanly packages all the logic and makes it very easy applicable. By just applying an attribute to a property or method I can add huge functionality to that member (resolver).

```csharp
public class Query
{
    /// <summary>
    /// This field says hello.
    /// </summary>
    [ToUpper]
    public string SayHello(string? name)
    {
        return name is null ? "Hello!" : $"Hello {name}!"
    }
}
```

This allows us to enable the full power of schema types with pure C# types. The new attributes will arrive with 10.3.0-preview.3 probably on Monday.

We will add attributes for each descriptor type. Moreover, you can apply input and output attributes on the same type, and we will create automatically an output- and an input-version of that type.

We will also provide attributes for all our middleware like paging, filtering, sorting and authorization. So, you will have the full power of Hot Chocolate even when you do not use our schema type directly.

> I really love this feature :)

## Type Extensions

Another thing we want to make better with 10.3.0 are the code-first type extensions. You could already do cool things with the type extensions but there are two things that did not feel nice enough.

First, we did not have a generic type extension type. This means that defining fields can sometimes be a pain. We had to either declare fields and provide the declaring type with them or we had to specify the field with a string name.

```csharp
public class FooExtension : ObjectTypeExtension
{
    protected override void Configure(ObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Field<Foo>(t => t.Bar).Use(...);
        descriptor.Field("baz").Use(...);
    }
}
```

With the new version we can now basically do the same than we do with standard types by providing a type parameter:

```csharp
public class FooExtension : ObjectTypeExtension<Foo>
{
    protected override void Configure(ObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Field(t => t.Bar).Use(...);
    }
}
```

The second thing that is sometimes good and other times bad is that we have to provide an name. With 10.3.0 we first of all can now infer the type from the mode.

You also can just type in the name like before. Or you provide as with the type that you want to actually extend.

```csharp
public class FooExtension : ObjectTypeExtension<Foo>
{
    protected override void Configure(ObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.Extend<FooType>()
        descriptor.Field(t => t.Bar).Use(...);
    }
}
```

## Optional

Another concept we will introduce with 10.3.0 is optionals. This is often a thing we want to use when we are talking about input types. We have introduced this concept already with _Strawberry Shake_ and like it a lot.

So, we are porting it now back to the server. In your resolvers you can now use for every argument the optional wrapper type and this will tell you if the argument was not provided. This will allow you to easily do partial updates. We could do partial updates before but not as elegant as now. With version 11 we will improve on that by having a nice patch type.

```csharp
public async Task<Foo> GetMyFoo(Optional<string> id)
{
    // ...
}
```

Also you can use optionals in input objects.

```csharp
public class Foo
{
    public Optional<string> Bar { get; set; }
}
```

The nice thing with the inputs are that they implicitly convert.

```csharp
var foo = new Foo { Bar = "My String" };
```

## Wrapping it up

Hot Chocolate 10.3.0 will bring a lot new improvements to how we can create GraphQL schemas. All these changes are just additions and there are no breaking changes involved meaning we give you a lot of version 11 productivity improvements now.

So, when can you expect 10.3.0. We will deliver nullable ref types with 10.3.0-preview.2 (tonight) and the attributes will come 10.3.0-preview.3. We think the final version should be ready end of next week. We initially planned end of this week but we still have some bug fixing to do.

I hope you are as exited as I am about this. Happy Thanksgiving :) and get a super awesome Hot Chocolate with marshmallows to get into your GraphQL groove.

If you want to get into contact with us head over to our [slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and join our community.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
