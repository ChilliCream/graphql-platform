---
title: Code-First
---

The code-first schema approach lets us build GraphQL schemas with .NET types and it gives us all the goodness of strong types and the confidence of using the C# compiler to validate our code. There is no need to switch to the GraphQL syntax in order to specify our schema, we can do everything in our favorite .NET language.

Let us get started and have a look at the various approaches that we can use to build a schema. It is important to know that we can mix the various approaches with Hot Chocolate and use the best solution for a specific problem.

# Pure Code-First

We call the first approach pure code-first since we do not bother about GraphQL schema types, we will just write clean C# code that automatically translates to GraphQL types.

> In order to use this approach in the most effective way opt into C# nullable reference types.

In GraphQL everything starts with one of the three root types (Query, Mutation or Subscriptions). Root types represent the operations that we can do on our schema.

So, if we wanted to create the query root type we would simply write a `Query` class.

```csharp
public class Query
{
    public string Hello() => "world";
}
```

Now let us create a new schema with that root type.

```csharp
var schema = SchemaBuilder.New()
  .AddQueryType<Query>()
  .Create();
```

We now have registered an object type with our new schema that is based on our `Query` class. The schema would look like the following (if nullable reference type are turned on):

```sdl
type Query {
  hello: String!
}
```

We didn't even have to write resolvers due to the fact that the schema inferred those from the hello method. Our hello method is actually our resolver.

This is just a simple class, with no real challenge to it. The schema builder is able to automatically infer interface usage, arguments, really everything just from our types.

But what if we wanted to apply middleware to our types like paging, filtering or sorting?

For this we have something called _descriptor attributes_ so applying filtering to a field would look like the following example:

```csharp
public class Query
{
    [UseFiltering]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

This attribute would add all the necessary filter input types and apply the filter middleware to that field.

If you want to read more about how to use or build these attributes head over [here](/docs/hotchocolate/v10/schema/descriptor-attributes).

# Code-First

The second and original approach to code-first is by using our schema types.

Schema types allow us to keep the GraphQL type configuration separate from our .NET types. This can be the right approach when we do not want any Hot Chocolate attributes on our business objects.

As I said earlier we can mix these approaches which can enable us to achieve awesome complex schemas with minimal boilerplate code.

Schema types can be created either by using the schema builder and add the configuration where we add the type ...

```csharp
var schema = SchemaBuilder.New()
    .AddQueryType<Query>(d => d
        .Field(f => f.Hello())
        .Type<NonNullType<StringType>>())
    .Create();
```

.. or, since these fluent chains could get very long and unreadable we could also opt to declare a new class `QueryType` that extends `ObjectType<Query>` and add it to our schema like the following.

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(f => f.Hello()).Type<NonNullType<StringType>>();
    }
}

var schema = SchemaBuilder.New()
    .AddQueryType<QueryType>()
    .Create();
```

Furthermore, we can add fields that are not based on our .NET type `Query`.

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(f => f.Hello()).Type<NonNullType<StringType>>();
        descriptor.Field("foo").Type<StringType>().Resolver(() => "bar");
    }
}
```

Our new resulting schema would now look like the following:

```sdl
type Query {
  hello: String!
  foo: String
}
```

The `foo` field would use the specified delegate to resolve the field value. The fluent API offers us the same feature set as the GraphQL schema syntax.

Next, let us have a look at resolver arguments. GraphQL fields let us define arguments, so they are more like methods in C# than properties.

If we add a parameter to our `Hello` method, the `SchemaBuilder` will translate that into a GraphQL field argument.

```csharp
public class Query
{
    public string Hello(string name) => $"Greetings {name}";
}
```

```sdl
type Query {
  hello(name: String!): String!
}
```

In order to get access to the resolver context in our resolver, we can just add the `IResolverContext` as a method parameter and the query engine will automatically inject the context:

```csharp
public class Query
{
    public string Hello(IResolverContext context, string name) =>
        $"Greetings {name} {context.Service<FooService>().GetBar()}";
}
```

This was just a quick introduction - There is a lot more that we can do with _pure code-first_ and _code-first_. In order to learn more, head over to the following documentation articles:

- If you want to read more about the `SchemaBuilder` head over [here](/docs/hotchocolate/v10/schema).

- If you are interested about resolvers in more detail [this](/docs/hotchocolate/v10/schema/resolvers) might be the right place for you.

- If you want to know how to split up types then [this](/docs/hotchocolate/v10/schema/splitting-types) might be what you are looking for.

You are all fired up and want to get started with a little tutorial walking you through an end-to-end example with `MongoDB` as your database? [Follow me](/docs/hotchocolate/v10/tutorials)!

OK, OK, you already have an idea on what to do and you are just looking for a way to setup this whole thing with ASP.NET Core? [This](/docs/hotchocolate/v10/server) is where you find more on that.

If you want to set Hot Chocolate up with AWS Lambda or Azure Functions head over to our slack channel, we do not yet have documentation on that but there are example projects showing how to do that. We are constantly adding to our documentation and will include documentation on that soon.
