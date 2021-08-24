---
path: "/blog/2019/02/04/hot-chocolate-0.7.0"
date: "2019-02-04"
title: "GraphQL - Hot Chocolate 0.7.0"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we have released Hot Chocolate version 0.7.0 which brings a lot of new features, improvements and bug fixes. With this post I walk you through the major changes.

The main focus of this release was to make the execution engine more extendable.

The execution engine in version 0.6.0 was closed and as a user of Hot Chocolate you didn't really have any chance to change it's behavior.

The only way to write field middleware components was through directives. With our new release this will fundamentally change.

## QueryExecutionBuilder

With version 0.7.0 we opened up the field middleware pipeline to be extended.

Moreover, we broke the query execution pipeline into query middleware components that can be swapped out or extended by writing a query middleware.

This all can be done with the new `QueryExecutionBuilder` that provides a simple to use API to customize how the query executor works.

```csharp
  IQueryExecutor executor = QueryExecutionBuilder.New()
    .Use(next => context =>
    {
      // ...
    })
    .UseDefaultPipeline()
    .Build(schema);
```

Instead of using the default pipeline we can also add the included middleware components one by one and swap out the ones that we do want to replace.

```csharp
  IQueryExecutor executor = QueryExecutionBuilder.New()
    .AddOptions(options)
    .AddErrorHandler()
    .AddQueryValidation()
    .AddDefaultValidationRules()
    .AddQueryCache(options.QueryCacheSize)
    .AddExecutionStrategyResolver()
    .AddDefaultParser()
    .Use(next => context =>
    {
      // ...
    })
    .UseInstrumentation(options.TracingPreference)
    .UseRequestTimeout()
    .UseExceptionHandling()
    .UseQueryParser()
    .UseValidation()
    .UseOperationResolver()
    .UseMaxComplexity()
    .UseOperationExecutor();
    .Build(schema);
```

On top of the new execution pipeline we build features like:

- Apollo Tracing
- Schema Stitching
- Pagination Support

More about this can be read [here](https://hotchocolate.io/docs/middleware).

## Syntax Rewriter

We also invested in our parser and added a lot of visitor and rewriter base classes that make working with the syntax tree less effort.

**What are visitors and rewriter good for?**

We started really thinking about this feature when we conceived the new schema stitching. We wanted to branch of parts of the query and rewrite them to become a query for another schema that is located somewhere else.

Rewriters are basically visitors that walk the graph and as they do that create a new query. Basically you pass in a syntax node and the rewriter returns a new syntax node that represents the rewritten node.

```csharp
FieldNode newField = rewriter.Rewrite(originalField);
```

This can be very useful if we want to map a graph to a database or create something like a schema stitching layer etc.

More about this can be read [here](https://hotchocolate.io/docs/parser).

## GraphQL Spec State

With version 0.7.0 we have added support for repeatable directives. This feature is slated for the next GraphQL spec version and allows to pipeline directives like the following:

{
a @fetch @replace('a' 'b') @replace('b' 'c')
}

This behavior feels really awesome when you use executable directives, since with this you can build the field resolver pipeline by stacking directives together.

_Directives are per default non-repeatable._

## Error Filter

One of the regular questions users had was about how to handle custom exceptions with Hot Chocolate.

With exception filters we now provide you with a simple way to do just this.

The execution engine will transform any exception thrown into a generic GraphQL error.

With exception filters you can then rewrite those errors for certain exceptions in order to provide more useful information.

More about this can be read [here](/docs/hotchocolate/v10/execution-engine/error-filter).

## Schema Stitching

On top of the execution improvements we built our new schema stitching capabilities. With those you are able to easily fuse service endpoints together.

More about this can be read [here](/blog/2019/01/24/schema-stitching).

## Apollo Tracing

With version 0.7.0 we have introduced diagnostic sources that can be used to add custom tracing and diagnostic solutions.

Furthermore, we now support [Apollo Tracing](https://github.com/apollographql/apollo-tracing). Apollo Tracing can be opted in by setting the tracing preference on the execution options. We recommend to switch it to on-demand, which allows you to send a header when ever you want to get performance performance information about a call.

## Relay and Paging

We made creating relay compliant schemas a lot easier with this release. We introduced the paging structures as well as the node interface.

Relay compliant paging can be done with one line of code if your data is provided by `IQueryable<T>`.

```csharp
descriptor
  .Field(t => t.GetCustomers)
  .UsePaging<CustomerType>();
```

Moreover, we have introduced a middleware that makes your IDs schema unique like required by the relay server specs without you having to implement any of that.

We will follow up this post with a post on how to best build schemas for relay.

More about paging can be found [here](https://hotchocolate.io/docs/pagination).

## Type Conversion

Until now the type conversion logic of Hot Chocolate was not accessible by the developer. This caused a lot of frustration since we were not able to add custom type conversions in a transparent way. So, basically the user had to add this code into his/her resolver logic. This felt like clutter that should not be there.

We have now introduced a new type conversion API.

Let us say you are working with mongo and you want to add an `ObjectId` conversion that basically converts `string` to `ObjectId` and `ObjectId` to `string`.

```csharp
TypeConversion.Default.Register<string, ObjectId>(from => ObjectId.Parse(from));
TypeConversion.Default.Register<ObjectId, string>(from => from.ToString());
```

So, that basically settles it. Two lines of code an you are done. You can also implement `ITypeConverter` in order to accommodate more complex code or just because you want to have your converters in class form.

Furthermore, we can create a new `TypeConversion` instance that only contains our specified conversion logic and none of our default converters in order to have tight control over them.

In this case we add the `TypeConversion` instance to our dependency injection and the execution engine will prefer the one provided via dependency injection over `TypeConversion.Default`.

## DataLoader

We already provided an API for writing _DataLoader_ but due to feedback from the community we rewrote our implementation to make it easier to use. You can now write _DataLoader_ with a single line of code by providing us with a delegate that fetches your data.

An example project that shows the new _DataLoader_ can be found [here](https://github.com/ChilliCream/hotchocolate-examples/tree/master/DataLoader).

Or head over to our documentation [here](https://hotchocolate.io/docs/dataloaders).

## Scalar Types

We removed our extended scalars from the base setup. This means that you now have to tell your schema to use these.

```csharp
Schema.Create(c =>
{
    c.RegisterExtendedScalarTypes();
});
```

This gives you more control about your type system and allows you to implement your own version of long etc.

More about scalar types can be found [here](https://hotchocolate.io/docs/custom-scalar-types).

## Generic InterfaceType and UnionType

The generic `InterfaceType` allows you to assign a .Net interface to a GraphQL interface. All object types that then have a .Net type associated will automatically implement this interface if the .Net type implements the .Net interface. Confused :)

Let`s see some code:

```csharp
public class FooType : InterfaceType<IFoo>
{

}
```

If we would do nothing else we will infer the fields from the interface.

If we now had the following type:

```csharp
public class Bar : IFoo { }

public class BarType : ObjectType<Bar>
{

}
```

Then we do not explicitly need to point to the interface anymore since we can infer the usage of the interface.

The same works for generic union types where you now can use marker interfaces to assign types to a set. For our purists that only want to you .Net types the following works now to:

```csharp
Schema.Create(c =>
{
    c.RegisterType<IFoo>();
    c.RegisterType<Bar>();
});
```

## Source Code Link

We now support NuGet source code link. This means that you can debug into the Hot Chocolate source. This is often a great help when you are struggling with a bug or do want to check whats happening.

## What`s comming next

Version 7 was a big release with a lot of new features that make it very easy to setup a GraphQL schema in .Net. With this release out we now focus on Version 8 which will focus on schema stitching. We will introduce capabilities like auto-stitching and auto-mocking. We already started working on the new schema stitching stories and if you think you would like to contribute ideas or code or documentation just feel free to talk to us. We are quite happy for any help.

After the schema stitching enhancements we will focus on the new schema builder with Version 9. The schema builder will bring in completely new capabilities that let you extend the schema building process. We are basically opening up the schema building process like we did with the execution engine.

[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
