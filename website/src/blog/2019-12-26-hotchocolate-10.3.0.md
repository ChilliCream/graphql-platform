---
path: "/blog/2019/12/26/hot-chocolate-10.3.0"
date: "2019-12-26"
title: "GraphQL - Hot Chocolate 10.3.0"
featuredImage: "shared/hotchocolate-banner.png"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we are releasing Hot Chocolate version 10.3.0. Although the version number sounds like a small change, it is quite a nice update with lots of new features making Hot Chocolate the most versatile and feature rich GraphQL server on the .NET platform.

We are now working for a long time on version 11. Work on that has begun long before version 10.0.0 was finished. As we progressed with version 11, we felt that we could push some nice productivity features down to the version 10 branch and make users of Hot Chocolate much happier.

This decision culminated in version 10.3.0 and it really feels like a major update with an array of new possibilities that will make you smile.

With version 10.3.0 we are introducing a new code-first variant which we internally call _pure code-first_.

We now really can for the first time build a fully-fledged GraphQL server just with C#.

> If you want to see how the Star Wars example looks like with the new 10.3.0 and _pure code-first_ then head over [here](https://github.com/ChilliCream/hotchocolate-examples/tree/master/PureCodeFirst).

Let`s dive into the features and explore what we can do with the newest version of Hot Chocolate.

## Nullability

The first feature that I want to introduce is C# 8 nullable reference type support.

With previous versions of C# we always had the problem that C# had only nullable reference types, hence we had to give our classes always some extra context to be able to infer non-null GraphQL types.

```csharp
public class Query
{
    /// <summary>
    /// This field says hello.
    /// </summary>
    [GraphQLNonNull]
    public string SayHello(string name)
    {
        return name is null ? "Hello!" : $"Hello {name}!"
    }
}
```

It is needless to say that we also could do that with our schema types.

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.SayHello(default)).Type<NonNullType<StringType>>();
    }
}
```

With C# 8.0 _Microsoft_ introduced a new language feature called nullable reference types that allows us to define when reference types can be null.

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

When activated either setting the _MSBuild_ property `<Nullable>enable</Nullable>` or by adding a preprocessor directive `#nullable enable` Hot Chocolate will automatically infer the nullability of GraphQL types from the corresponding .NET types.

Hence the above class is now correctly inferred and translates nicely into GraphQL types.

```graphql
type Query {
  "This field says hello."
  sayHello(name: String): String!
}
```

## Descriptor Attributes

One big issue that we still saw with _pure code-first_ was how people should apply middleware to their fields. This was for a long time a roadblock for us in making this experience more powerful and easy to use.

Our solution to this are descriptor attributes which act as a kind of an interceptor into the inferred schema type. This allows users to create their own attributes in an easy way and with all the power that is available through the schema type APIs.

```csharp
public sealed class ToUpperAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
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

The attributes very cleanly package all the logic for a middleware or other configuration aspects. This makes it very easy to use. By just applying an attribute to a class, property, method or any other member kind we can add completely new functionality to that specific element or even completely reconfigure it.

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

We have created attribute base classes for all the important descriptors.

- EnumTypeDescriptorAttribute
- EnumValueDescriptorAttribute
- InputObjectTypeDescriptorAttribute
- InputFieldDescriptorAttribute
- InterfaceTypeDescriptorAttribute
- InterfaceFieldDescriptorAttribute
- ObjectTypeDescriptorAttribute
- ObjectFieldDescriptorAttribute
- ArgumentDescriptorAttribute
- UnionTypeDescriptorAttribute

But sometimes we even want to drill deeper with attributes and use a single attribute with multiple descriptors.

Maybe we only want to apply arguments through an attribute to a field if the field is on an interface.

```csharp
public interface IFoo
{
    [UseOffsetPaging]
    IQueryable<IFoo> GetFoos();
}
```

```graphql
interface Foo {
  foos(skip: Int, take: Int): [Foo!]!
}
```

But if the same attribute is applied to an object field then we might also want to apply a middleware that adds some cross-cutting functionality to it like a paging algorithm.

```csharp
public interface Bar : IFoo
{
    [UseOffsetPaging]
    IQueryable<IFoo> GetFoos();
}
```

```graphql
type Bar implements Foo {
  foos(skip: Int, take: Int): [Foo!]!
}
```

For this we can use the attribute base class `DescriptorAttribute`.

```csharp
public sealed class UseOffsetPagingAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (element is MemberInfo m)
        {
            if (descriptor is IObjectFieldDescriptor ofd)
            {
                // do something
            }
            else if (descriptor is IInterfaceFieldDescriptor ifd)
            {
                // do something
            }
        }
    }
}
```

The `TryConfigure` method passes in the `IDescriptorContext` which provides us access to conventions and other services. Also, we have access to the `descriptor` that is associated with the annotated element. Additionally the `element` to which the attribute is annotated to is also passed in.

With this it is very easy to probe for different cases and build complex functionality in a simple attribute that is easy to use by others.

Last but not least we also have added a set of built-in attributes for paging, filtering, sorting and authorization.

```csharp
public class Query
{
    /// <summary>
    /// This field says hello.
    /// </summary>
    [Authorize(Policy = "MyPolicy")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Customer> GetCustomers()
    {
        ...
    }
}
```

The attributes can be chained just like with the fluent API. The above code would translate into the following schema type.

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Field(t => t.GetCustomers())
            .Description("This field says hello.")
            .Authorize("MyPolicy")
            .UsePaging<ObjectType<Customer>>()
            .UseFiltering()
            .UseSorting();
    }
}
```

## Type Attributes

Another issue that we had was telling the schema builder that we want to force types to bind as specific GraphQL type.

A class for instance is automatically inferred as an object type when the type is discovered in an output context. But a `struct` on the other hand is not automatically inferred if it is not mapped as a scalar since it could become quite messy with distinguishing if a `struct` should become a scalar or an object type or an input object type.

For this problem we have created a special set of descriptor attributes that mark the .NET type as a specific GraphQL type.

```csharp
[ObjectType(Name = "QueryRoot")]
public struct Query
{
    public string Foo => "Foo";
}
```

The same would work if we wanted to enforce that an abstract base class for instance becomes an interface or even a union type. It is important that the context in which this type is discovered also matters. So, one type could translate into an input type and an output type at the same time.

```csharp
public class Query
{
    public Foo GetFoo(Foo foo) => foo;
}

public class Foo
{
    public string Bar { get; set; }
}
```

The above example would automatically translate into a GraphQL schema where `Foo` would be represented by two types in the GraphQL schema.

```graphql
type Query {
  foo(input: FooInput): Foo
}

type Foo {
  bar: String
}

input FooInput {
  bar: String
}
```

OK, it starts to feel quite nice :)

But still we are not there yet.

When people start building big APIs, they tend to want to split up types. The most asked question on our slack channel is how to split up the query type.

With SDL-first and our traditional code-first approach this is as easy as eating pie since we can write type extensions.

So, we added for 10.3.0 the ability to also split up types with the _pure code-first_ approach.

Let\`s say we have a query type and we want to divide this up into logical units. We could add a bodiless query type by either adding an empty class to our `SchemaBuilder` or by using a schema type.

**Approach 1 - Empty Class**

```csharp
public class Query
{
}

SchemaBuilder.New()
    .AddQueryType<Query>()
    ...
```

**Approach 2 - Schema Type**

```csharp
public class Query : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");
    }
}

SchemaBuilder.New()
    .AddQueryType<QueryType>()
    ...
```

Next we could create standard C# classes to extend on the query type. We can divide our type into as many classes as we want. Also, since each class is independent we could for instance have extra query fields during development time by just adding an extension class on dev to our schema builder and on prod we could leave that away.

```csharp
[ExtendObjectType(Name = "Query")]
public class FooQueries
{
    public string Hello() => "abc";
}

SchemaBuilder.New()
    .AddQueryType<QueryType>()
    .AddType<FooQueries>()
    .Create();
```

The above code would result in the following schema:

```graphql
type Query {
  hello: String
}
```

Object type extensions let us divide our GraphQL types into multiple .NET types. This lets us be more flexible in building our API. Moreover, we can divide our query type into logical units and test them independently from each other. We can do that by just writing a clean C# class that only really would need one attribute to mark it as an extension.

## Interfaces

Hot Chocolate is able to infer interface types from C# APIs since version 10.0.0. But now with the new capabilities of Hot Chocolate in 10.3.0 this becomes a really great feature.

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

This feels awesome. The schema builder translates our C# types exactly the way we meant them into a clean GraphQL schema. We do not have to write all those schema types anymore. We just write clean C# code and let the schema builder handle the rest.

> It is important to know that we still can use schema types. Also, we can mix our approach, for instance we could use schema types in situations where we do not want to add attributes to our types.

## Optional

Another concept we are introducing with 10.3.0 is optional on input object types. We are planning to use optional even more with version 11 but with 10.3.0 you can use them on input object types in order to distinguish between **not set** and **null**.

```csharp
public class Foo
{
    public Optional<string> Bar { get; set; }
}
```

The important thing with optional is that they implicitly convert to the type specified as type parameter. This means that the following is valid code:

```csharp
var foo = new Foo { Bar = "ABCDEF" };
string fooValue = foo.Bar;
```

But we also can now distinguish between **not set** and **null** since we can ask the optional if it has a value.

```csharp
var foo = new Foo { Bar = "ABCDEF" };
if(foo.Bar.HasValue)
{
    // property was set.
}
```

Optional in 10.3.0 only work on properties of input objects meaning we cannot use them on output types. With 10.3.0 the execution engine has no knowledge about optional at all.

Also, we cannot use optional on arguments in the way that we could ask the context for an optional like the following:

```csharp
context.Argument<Optional<string>>("foo");
```

We will introduce this with the upcoming version 11 release. We decided to not change the execution engine to much with 10.3.0 since we are doing a lot of work on the execution engine with version 11.

Another caveat here is that if you are using `Optional<T>` on a property, the property cannot have a default value. This is also one thing we will change with version 11.

Still, optional can help already in version 10.3.0 with some scenarios and with version 11 we will go all the way to make this an awesome addition.

## Type Extensions

For the last few paragraphs I only talked about the _pure code-first_ approach but we actually also added a new feature to the schema types (aka _code-first_ approach).

For a long time now, we can extend types or break types up into multiple parts.

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

But the type extension until now did not allow to specify an underlying model. With 10.3.0 we now allow you to specify any type extension with a generic type parameter.

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

## Immutable Input Objects

We are not done yet :) There are still more features on 10.3.0.

Another feature we have integrated into the input object types is support for immutable input objects. This becomes important when working with C# 8.0 and nullable reference types.

With version 10.3.0 we can now specify immutable classes like the following one as input object.

```csharp
public class ImmutableFoo
{
    public ImmutableFoo(string bar)
    {
        Bar = bar;
    }

    public string Bar { get; }
}
```

If we use the above class as input object the type can deserialize or parse it correctly by using the constructor instead of setting the properties.

Also supported is to use the constructor just for non-null reference types like the following:

```csharp
public class Foo
{
    public Foo(string bar)
    {
        Bar = bar;
    }

    public string Bar { get; set; }

    public string? Baz { get; set; }
}
```

## Subscriptions

Last but not least we did some work to make subscriptions easier and allow people to leverage the power of async streams.

If you are happy with subscriptions today, you do not need to change anything.

But if you want to easily hook up Azure ServiceBus or stream something over what ever, then this has become super simple with the new subscribe resolver.

```csharp
public class SubscriptionType : ObjectType
{
    protected override void Configure(ObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.Field("foo")
            .Subscribe(async ctx =>
            {
                async foreach(var payload in await serviceBus.OnMessageReceiveAsync())
                {
                    yield return payload;
                }
            })
            ...
    }
}
```

You also can bind the subscribe resolver like any other resolver to an underlying method.

```csharp
public class SubscriptionType : ObjectType<Subscription>
{
    protected override void Configure(ObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor
            .Field(t => t.GetMessageAsync())
            .Subscribe(t => t.OnReceiveMessage())
            ...
    }
}
```

The subscribe resolvers accepts `IAsyncEnumerable<T>`, `IEnumerable<T>` and `IObservable<T>` as result.

## Wrapping it up

With Hot Chocolate 10.3.0 we focused on productivity features that have a minor impact on the overall system. This means that we enable a whole bunch of new scenarios with the current Hot Chocolate server generation.

With version 11 we will take this to a whole new level with a completely new execution engine that is much more efficient and allows for completely new features like `@defer`.

Also, version 11 will introduce new tools and libraries to the platform like _Banana Cakepop_ (preview dropping very soon), _Strawberry Shake_ or our new _Visual Studio for Windows Integration_.

We have a lot more in our pipeline and are totally obsessed with GraphQL and .NET.

I hope you will enjoy 10.3.0 as much as I already do and join the Hot Chocolate fold.

BTW, head over to our _pure code-first_ [Star Wars example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/PureCodeFirst).

If you want to get into contact with us head over to our [slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and join our community.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
