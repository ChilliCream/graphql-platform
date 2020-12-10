---
path: "/blog/2019/04/12/type-system"
date: "2019-04-12"
title: "GraphQL - Hot Chocolate 9.0.0 - Type System"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Originally, I wanted to write a little post about what we are currently working on in version 9 and how those things are coming along, but every time I started writing on this post it got longer and longer and it felt a bit too messy.

Starting with this post we will start talking about version 9 in more detail. We will split this into several blog posts that will cover different parts of the new version. This post will focus on the type system improvements.

We started with version 9.0.0-preview.9 to deliver more and more parts of the new type system. With version 9.0.0-preview.11 we are delivering a ton of bug fixes and many more new features.

## Schema Builder

The most prominent API that we are introducing is the new `SchemaBuilder`. The `SchemaBuilder` provides us with a new way to define schemas. Do not worry the current `ISchemaConfiguration` API is still supported and will not go away. In fact, `ISchemaConfiguration` now is just an interface over `SchemaBuilder` and we will evolve both APIs over time so that you can pick the one that you like more.

```csharp
ISchema schema = SchemaBuilder.New()
    .AddQueryType<FooType>()
    .Create();
```

**So, why did we introduce a new API to define a schema?**

First, we wanted the builder API to be decoupled from the actual schema, we wanted to be able to start adding schema types and other parts to a schema builder without being forced to create the schema.

With the schema builder we are now more flexible in scenarios like schema stitching.

```csharp
ISchema schema = SchemaBuilder.New()
    .AddQueryType<FooType>()
    .AddDirectiveType<BarType>()
    .AddSchemaFromFile("./Schema.graphql")
    .AddContextData("foo", "bar")
    .ModifyOptions(options => {  })
    .AddServices(services_a)
    .AddServices(services_b)
    .Create();
```

## Conventions

With our new schema builder, we did a lot of work underneath and introduced the ability to use services during type construction.

**For what is that useful?**

For one you can now register our new `INamingConverions` with the dependency injection and then the new `SchemaBuilder` will use your naming conventions instead of the built-in naming conventions.

Also, you can register our new `ITypeInspector` and override how we infer schema types from POCOs. This will allow you for instance to add support for custom attributes, so no need to pollute your API with our attributes anymore.

But fear not, you do not have to implement the whole `INamingConverions` interface for instance since you can override each part of our default implementation.

Since, in many cases we just want to tune existing naming conventions we can inherit from the default implementation `DefaultNamingConventions` and overwrite just what we want to change.

So, if we wanted to add to all the input type names the prefix `super` we could do this like the following:

```csharp
public class MyNamingConventions
{
    public override NameString GetTypeName(Type type, TypeKind kind)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (kind == TypeKind.InputObject)
        {
            if (!name.EndsWith("Super", StringComparison.Ordinal))
            {
                name = name + "Super";
            }
        }

        return base.GetTypeName(type, kind);
    }
}
```

Like with the naming conventions we provide a default implementation to `ITypeInspector` where we can replace or extend parts that we want to modify.

In order to register our conventions with the schema builder we can do the following:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<INamingConverions, MyNamingConventions>();
    services.AddGraphQL(sp => Schema.Create(c =>
    {
        c.RegisterServiceProvider(sp);
        c.RegisterQuerType<Foo>();
    }));

}
```

Or we could do it like the following with the new schema builder:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<INamingConverions, MyNamingConventions>();
    services.AddGraphQL(sp => SchemaBuilder.New()
        .AddQueryType<Foo>()
        .AddServices(sp));
}
```

## Extending Types

One other major reason to rethink our type system was that many of you wanted to extend on types. One common thing that people wanted to do is to introduce generic types. We did something like this with our relay types. GraphQL does not really have generic types but the idea here is that you could have a type like the following:

```csharp
public class EdgeType<TSchemaType>
    : ObjectType<IEdge>
    where TSchemaType : IOutputType
{
}
```

If we for instance put in the `StringType` as TSchemaType then the edge type would become `StringEdge` in the schema. While this is not so difficult if our `StringType` has a fixed name, it becomes more difficult if `StringType` would create its name also depending on another type.

With version 9 we redesigned the schema initialization process so, that you can register dependencies for a type with the `SchemaBuilder`. This way the `SchemaBuilder` knows which type has to be initialized in which order.

So, let us have a look at how we would create our edge type with version 9:

```csharp
public class EdgeType<TSchemaType>
    : ObjectType<IEdge>
    where T : IOutputType
{
    protected override void Configure(
        IObjectTypeDescriptor<IEdge> descriptor)
    {
        descriptor.Name(dependency => dependency.Name + "Edge")
            .DependsOn<TSchemaType>();
    }
}
```

With the new `Name` extension on the type descriptors we are now able to define a delegate that represents the naming algorithm for that type. Moreover, we can now express on which type this algorithm depends.

This new `Name` descriptor extension is built upon our new descriptor extension API that provides a new way to extend our descriptors without needing to create a new base class.

### Extending Descriptors

Each descriptor now provides a new method called `Extend`. `Extend` returns an extension descriptor that allows us to integrate some logic with the type initialization pipeline.

Types are created in three phases:

- Create Instance
  The initializer creates the type instance and the type definition.
  The type definition contains all information to create and initialize a type.
  After this step the type instance exists and is associated with a native .net type.
  The native .net type can be object but can also be something more specific.
  In this phase the type will also report all of its dependencies to the schema builder.

- Complete Name
  After all types are created the names of the types will be completed.

- Complete Type
  In the last step the types will be completed, this means that for instance the fields are assigned, or the directives are retrieved and associated with a type etc.
  After this the type is completed and becomes immutable.

The extension descriptor provides extension points to these three phases:

- OnBeforeCreate
  OnBeforeCreate will allow you to customize the type definition.
  It is important to know that this step is not allowed to be dependent on another type object. Also, at this point you will not have access to the type completion context.

- OnBeforeNaming
  OnBeforeNaming allows to provide logic to generate the name of a type.
  You can declare two kinds of dependencies in this step, either the dependency has to be named first or the dependency is allowed to be in any state.

- OnBeforeCompletion
  OnBeforeCompletion allows to provide further logic that modifies the type definition. For instance, we could be dependent on another type in order to generate fields based on the fields of that other type.
  You can declare two kinds of dependencies in this step, either the dependency has to be completed first or the dependency is allowed to be in any state.

Let us have a look at how we implemented our own `Name` extension method in order to understand what `Extend` is useful for:

```csharp
descriptor
  .Extend()
  .OnBeforeNaming((ctx, definition) =>
  {
      INamedType type = ctx.GetType<INamedType>(
          ClrTypeReference.FromSchemaType(typeInfo.ClrType));
      definition.Name = createName(type);
  })
  .DependsOn(dependency, mustBeNamed:true);
```

Let us pic that example apart in order to understand what we did here. First, we called `Extend`, `Extend` returns the `IDescriptorExtension<T>` which allows us to register some code with the descriptor events that I have described earlier.

Each event will provide us with the type definition and the completion context. The `ICompletionContext` is the API to request information from the schema builder. In the case of our `Name` extension we are requesting the type instance for our `TSchemaType`. After that we call the naming algorithm with the resolved schema type.

Also, we added a dependency with `DependsOn`. The Boolean argument on `DependsOn` declares that the type has to be named before our delegate can be executed. We can declare as many dependencies as we want, so we are not bound to have just one.

Let me sum that up. The new `Extend` method on the descriptors allow us to extend the type descriptors without the need to create a new type base class. This is nice because you can now create extension methods that work across multiple solutions without forcing the user of that extension to opt into a new type base class. This makes it easy to consume those extensions. It is important to know here that `Extend` is available on all descriptors, so it is available on field descriptors, argument descriptors, or type descriptors.

### Replacing Descriptors

Though `Extend` is very capable, in some cases we might want to limit what is available through our descriptor. This basically means we want to remove functionality or replace the descriptor entirely. Let us assume we want to introduce an input type that describes the filter capabilities that can be applied to an output type. Basically, we want to introduce a filter input type like Prisma does.

So, if we had a type like the following:

```graphql
type Foo {
  bar: String!
}
```

We would want to be able to describe the filter capabilities that are available to the user of our API. This could look something like the following:

```csharp
public class FooFilterType
    : FilterType<Foo>
{
    public void Configure(IFilterDescriptor descriptor)
    {
        descriptor.Filter(t => t.Bar).AllowSmallerThan();
    }
}
```

The `FilterType<Foo>` inherits from `InputObjectType` and can with version 9 add its own descriptor. In order to replace the descriptor on our input type we would have to replace the configure method and introduce our new filter descriptor:

```csharp
public class FilterType<T>
    : InputObjectType
{
    private readonly Action<IFilterTypeDescriptor<T>> _configure;

    public FilterType()
    {
        _configure = Configure;
    }

    public FilterType(Action<IFilterTypeDescriptor<T>> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    #region Configuration

    protected override InputObjectTypeDefinition CreateDefinition(
        IInitializationContext context)
    {
        var descriptor =
            FilterTypeDescriptor.New<T>(
                DescriptorContext.Create(context.Services));
        _configure(descriptor);
        return descriptor.CreateDefinition();
    }

    protected virtual void Configure(
        IFilterTypeDescriptor<T> descriptor)
    {
    }

    protected sealed override void Configure(
        IInputObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }

    #endregion
}
```

Like the descriptor extend logic we basically can override those three type initialization events.

In order to replace the old descriptor, we sealed of the old `Configure` method. Also, we introduced our new `Configure` method with the new descriptor.

```csharp
protected virtual void Configure(
    IFilterTypeDescriptor<T> descriptor)
{
}

protected sealed override void Configure(
    IInputObjectTypeDescriptor descriptor)
{
    throw new NotSupportedException();
}
```

In order to initialize our new descriptor, we overrode the `CreateDefinition` method. Our descriptor has to produce a `InputObjectTypeDefinition` in order to abide to the `InputType` interface. If you want your descriptor extendable like our descriptors, all you have to do is inherit from our descriptor base. With version 9 all descriptor and type definition classes are now public, and we strongly recommend basing your descriptors on our base classes.

## Context Data Support on Types

Also, with the new version we added the context data dictionary to all types, fields and arguments. You can use this to add custom metadata to objects of the type system. Context data can be declared on the type definition and will be copied to the corresponding type object.

```csharp
descriptor
  .Extend()
  .OnBeforeCreate(definition =>
  {
      definition.ContextData["Foo"] = "Bar";
  });
```

You can access the context data on a type object like the following:

```csharp
schema.GetType<ObjectType>("Query").ContextData.ContainsKey("Foo");
```

## Improved Relay Support

With version 9 we are making creating relay compliant schemas a breeze. Lets have a look at the relay server spec parts and see how those translate to Hot Chocolate:

In order to activate relayjs support you can do now the following:

```csharp
SchemaBuilder.New()
    .EnableRelaySupport()
    .AddQueryType<Foo>()
    .Create()
```

`EnableRelaySupport` will add the node field to your query type and setup the general logic of how your nodes will be resolved using an id value. Moreover, this activates the id value serialization and deserialization. The schema will now have opaque identifiers, but you will not have to deal with those in your API.

In `ObjectType`s you can now declare a type as node type. That means this type will implement the node interface and can be resolved through the node field:

```csharp
public class FooType
    : ObjectType<Foo>
{
    protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.AsNode<Foo,int>((ctx, id) =>
            ctx.Service<IMyRepository>().GetById(id));
    }
}
```

Ok, this is basically all you have to do to fulfill spec item `A mechanism for refetching an object.`.

The other spec items for the relay spec were already quite good with version 8. It felt always odd to expose so much logic about those node resolvers to the developers that we refined our current APIs. We used the new `Extend` mechanism to provide extensions that help you along the way without forcing you to use a special base class.

## Code-First Type Extensions

The last thing I want to talk about in this post are code-first type extensions. We already supported the `extend` keyword in the stitching layer but had no real code-first API for this. Also, we only supported this in the stitching layer. With version 9 you can now extend code-first and schema-first. Moreover, type extensions are not bound to the stitching layer and work also on a standard schema.

Let us say we have the type `FooType` that has one field `description`.

```csharp
public class FooType
    : ObjectType<Foo>
{
    protected override void Configure(
        IObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.Field(t => t.Description);
    }
}
```

We can now introduce a type extension for our `FooType` that adds for instance a new field `test`.

```csharp
public class FooTypeExtension
    : ObjectTypeExtension
{
    protected override void Configure(
        IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Field("test")
            .Resolver(() => new List<string>())
            .Type<ListType<StringType>>();
    }
}
```

The code-first extension types can do much more then, the schema-first variant. For instance, with code-first you can add middleware parts to a field replace or update a field, replace the resolver, add or replace directives on fields, arguments and so on. Also, you have all the extension functionality that you have on normal types. In fact, since the type extension and the type are using the same descriptor you can apply the same extensions to both.

Also, you can define multiple type extensions for a single type.

So, let us have a look of how we add type extensions to our schema:

```csharp
ISchema schema = SchemaBuilder.New()
  .AddQueryType<FooType>()
  .AddType<FooTypeExtension>();
  .Create()
```

The schema builder basically treats them as types, so there is nothing special that you have to do in order to register them.

As we go forward, we will also introduce generic variants of the extension types. This will be quite nice in the stitching layer since you can provide a .Net type that we will use to deserialize the object. This means that you can write your resolvers against strong types instead of the generic types that we use per default in the stitching layer.

## Wrapping it up

This is just the first bunch of features that are included with version 9. The best thing, all of what I have showed you today is already included in version 9.0.0-preview.11 which we have released alongside this blog post.

The next few posts will focus on execution plan support in our query engine. Execution plans can be cached and persisted and will make stitching so much faster. Also, we need the new execution plan feature to introduce support for `@defer`.

Furthermore, we will give a peek at our new high-performance parser.

Also, we will have a look at subscription stitching and our reworked subscription implementation that is now based on the pipeline API of .Net Core.

Last but not least, we hope we are be able to squeeze in our new `FilterType` feature with version 9.

As you can see version 9 will bring quite a few improvements, so stay tuned for our next post on V9 and try out our previews. Also, join our slack channel and give us your take on GraphQL, tell us what you would like to see next in Hot Chocolate.

With Hot Chocolate we are building a GraphQL server for the community, so join and help us along.

We value any kind of contribution, whether you give us a star, a feedback, find a bug, a typo, or whether you contribute code. Every bit matters and makes our project better.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
