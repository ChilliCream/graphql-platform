---
path: "/blog/2020/07/16/version-11"
date: "2020-07-16"
title: "Say hello to Hot Chocolate 12!"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "hotchocolate-where-is-v11-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we are releasing Hot Chocolate version 12, which brings many new features and refinements to the platform. The main focus for this release was to put a new execution engine in that will allow us to build a more efficient schema federation with the next version. We are constantly iteration on the execution engine to make it more efficient and allow for new use-cases. Many implementations for GraphQL federation/stitching build a specific execution engine for this particular case of schema federation. With Hot Chocolate, we always wanted to keep this integrated and allow for stitching part of a graph while at the same time extending types in that very same schema. This also allows us to use improvements made for schema federation in other areas like Hot Chocolate Data, which will boost features and reliability with the next release.

# Execution Engine

The execution engine is changing with every release of Hot Chocolate. With version 11, we, for instance, introduced operation compilation, which takes the executed operation out of a document and pre-compiles it so that most of the GraphQL execution algorithm can be skipped in consecutive calls.

// Diagram Execution 11

With Hot Chocolate 12, we now take this further by introducing a query plan; essentially, the execution engine traverses a compiled operation tree to create a query plan from it. The query plan can take into account how a resolver is executed. For instance, if a resolver uses services that can only be used by a single thread and thus need synchronization.

// Diagram Execution 12

Moreover, the execution engine can now inspect if it can pull up data fetching logic and inject a completed result into a resolver pipeline and, by doing this, optimize data fetching. Doing this sounds kind of like DataLoader since we are tackling batching with this in some way. But actually, it makes these implications visible to the executor. Query plans allow the executor to inspect these things before running a query, thus improving execution behavior.

Moreover, the execution engine now differentiates between pure and async resolvers. Pure resolvers are synchronous and only need what is available in their parent resolver. Such resolvers can now be inlined by the execution plan, allowing us to skip a lot of logic we usually would need to execute.

## Performance

We had this simple throughput test for Hot Chocolate 11, which essentially executes a simple query to fetch books and authors. Hot Chocolate 11 achieved 19.983 requests a second on our test hardware. Now, with the new execution engine, we clock in 30.000 requests a second are an additional 10.000 more requests per second with the same hardware on a test that does not even really take advantage of all the new optimizations.

Hot Chocolate 12 executes much faster but also saves on the memory used. The execution now needs x% less memory to execute, for instance, an introspection request.

// we need some more perf statistics here

# Entity Framework

I talked about a lot of improvements we put in the execution engine which we only will unlock with Hot Chocolate 13, but we also have some practical use for these with Hot Chocolate 12. Specifically for APIs that use Entity Framework in a specific way. In general I always recommend to let the execution engine roam free and parallelize as needed. With Entity Framework this can be achieved with DBContext pooling. But in some cases this is not what people want or need for their specific use-case.

With Hot Chocolate 12 you can now mark a resolver serial and by doing this tell the execution engine that we need to synchronize this resolver. This is needed when using a single DBContext for one request.

You can mark a single resolver as serial, or mark all async resolvers as serial by default.

In the annotation base approach we just need to annotate our resolver with the `SerialAttribute` to ensure that the execution engine will ensure that not executed in parallel.

```csharp
[Serial]
public async Task<Person> GetPersonByIdAsync([Service] MyDbContext context)
{
    // omitted for brevity
}
```

Moreover, as mentioned we can mark all async resolvers as serial by default.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(o => o.DefaultResolverStrategy = ExecutionStrategy.Serial)
```

Serial executable resolvers will be put into a sequence shape of the query plan and guarantee that they are executed one after the other. You can inspect the query plan by providing the `graphql-query-plan` header with a value of `1`.

If we head over to https://workshop.chillicream.com and run the following query with the query plan header we will get the following execution plan.

```graphql
{
  a: sessions {
    nodes {
      title
    }
  }

  b: sessions {
    nodes {
      title
    }
  }
}
```

```json
{
  "extensions": {
    "queryPlan": {
      "flow": {
        "type": "Operation",
        "root": {
          "type": "Resolver",
          "strategy": "Parallel",
          "selections": [
            {
              "id": 0,
              "field": "Query.sessions",
              "responseName": "a"
            },
            {
              "id": 1,
              "field": "Query.sessions",
              "responseName": "b"
            }
          ]
        }
      },
      "selections": "{\n  ... on Query {\n    a: sessions @__execute(id: 0, kind: DEFAULT, type: COMPOSITE) {\n      ... on SessionsConnection {\n        nodes @__execute(id: 4, kind: PURE, type: COMPOSITE_LIST) {\n          ... on Session {\n            title @__execute(id: 5, kind: PURE, type: LEAF)\n          }\n        }\n      }\n    }\n    b: sessions @__execute(id: 1, kind: DEFAULT, type: COMPOSITE) {\n      ... on SessionsConnection {\n        nodes @__execute(id: 2, kind: PURE, type: COMPOSITE_LIST) {\n          ... on Session {\n            title @__execute(id: 3, kind: PURE, type: LEAF)\n          }\n        }\n      }\n    }\n  }\n}"
    }
  }
}
```

Providing this header will add an extension property to the response with the query plan and the internally compiled operation. We can see that the query plan only has two fields in it, these are the async fields that really fetch data, all the other fields are folded into their parent threads. We also can see that these two resolvers can be executed in parallel. Depending on how many components are involved these query plans can be much bigger end expose the dependencies between the data fetching.

If we did the same for serial resolvers we would get a sequence shape as mentioned above that would execute resolver tasks one after the other.

BTW, allowing such serial execution flows in Hot Chocolate 12 was one of the most requested features, and the team is quite happy to provide these now to our community.

# Resolver Compiler

One of the things many people love about Hot Chocolate is how we infer the GraphQL schema from your C# types and how you can inject various things into your resolver.

```csharp
[Serial]
public async Task<Person> GetPersonByIdAsync([Service] MyDbContext context)
{
    // omitted for brevity
}
```

Lets take the above for instance, we are injecting a service `MyDbContext` into our resolver. The resolver compiler knows what to do because of the service attribute. But this can become tedious to always annotate all those parameters in all those resolvers. Further, people want to extend introduce maybe their own attributes or their own functionality into the resolver compiler. With Hot Chocolate 12 we now open up the resolver compiler and allow you to configure it in a very simple way.

Lets start with the very basic example where we want to have `MyDbContext` as a known service that does no longer need an attribute.

Essentially what we want is to just write the following:

```csharp
[Serial]
public async Task<Person> GetPersonByIdAsync(MyDbContext context)
{
    // omitted for brevity
}
```

In order to tell the resolver compiler that we have a common service that we use in many resolvers we just do the following:

```csharp
.AddGraphQLServer()
    .AddQueryType<Query>()
    .ConfigureResolverCompiler(r =>
    {
        r.AddService<Service>();
    });
```

Specifically for the service case we really simplified things with this nice `AddService` extension method. But what if we wanted to inject a specific thing form the request state. Essentially we want to grab something from the `ContextData` dictionary and make nicely accessible.

```csharp
[Serial]
public async Task<Person> GetPersonByIdAsync(MyDbContext context, CustomState state)
{
    // omitted for brevity
}
```

```csharp
.AddGraphQLServer()
    .AddQueryType<Query>()
    .ConfigureResolverCompiler(r =>
    {
        r.AddService<Service>();
        r.AddParameter<CustomState>(resolverContext => (CustomState)resolverContext.ContextData["myCustomState"]!);
    });
```

I know, the expression I wrote there is not safe, it is just an example. But it should give you an idea how easily you can now write compilable expression that we can integrate. All of these are compilable expressions, we will integrate the expression specified into the compiled resolver method.

Also we could go further and you could write a selector for your resolver compiler extension that really inspects the parameter. These inspections are run at startup so there is no overhead on the runtime.

```csharp
.AddGraphQLServer()
    .AddQueryType<Query>()
    .ConfigureResolverCompiler(r =>
    {
        r.AddService<Service>();
        r.AddParameter<CustomState>(
            resolverContext => (CustomState)resolverContext.ContextData["myCustomState"]!,
            p => p.Name == "state");
    });
```

Also, we are not done with this and are already thinking how to give you even more freedom by allowing to inject proper logic that can be run in the resolver pipeline. What we want to allow are kind of conditional middleware, where we will append middleware depending on what you inject into your resolver. We have not solved all the issues on this one and have moved this to Hot Chocolate 13.

# Dynamic Schemas

While static schemas created through C# or through GraphQL SDL are very simple to build with Hot Chocolate it was quite challenging to build dynamic schemas that are based on Json files or database tables. It was achievable, like in the case of schema stitching but it was quite difficult and you needed to know quite a lot about the internals. With Hot Chocolate 12 we are opening up the type system quite a lot to allow you to create types in an unsafe way.

With unsafe I mean that we allow you to create the types by bypassing validation logic for the default user and using the type system definition objects that we internally use to configure the types. I will do a followup post that goes deeper into the type system and how it works in Hot Chocolate.

For this post let me show yo a simple example of how you now can create dynamic types. First let me introduce a new term here. With Hot Chocolate 12 we are introducing a new component call type module.

```csharp
/// <summary>
/// A type module allows you to easily build a component that dynamically provides types to
/// the schema building process.
/// </summary>
public interface ITypeModule
{
    /// <summary>
    /// This event signals that types have changed and the current schema
    /// version has to be phased out.
    /// </summary>
    event EventHandler<EventArgs> TypesChanged;

    /// <summary>
    /// Will be called by the schema building process to add the dynamically created types to
    /// the schema building process.
    /// </summary>
    /// <param name="context">
    /// The descriptor context provides access to schema building services and conventions.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a collection of types that shall be added to the schema building process.
    /// </returns>
    ValueTask<IReadOnlyCollection<INamedType>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken);
}
```

-> Type Modules -> Added support for type modules.
-> UnsafeCreate
-> TypeInterceptors

DataLoader
-> caching
-> update dl cache
-> Moved DataLoader code out of `HotChocolate.Types` into `GreenDonut`. (#4015)

Cursor Paging
-> Introduced option to require paging boundaries #4074
-> Add more capabilities to control how the connection name is created #4081

Validation

Middleware

- Order Validation

Relay

- nodes field
- Split the` EnableRelaySupport` configuration method into two separate APIs that allow to opt-into specific relay schema features. (#3972)

AggregateError
Enhanced error handling for variables to better pinpoint the actual error

ASP.NET Core improvements

Schema-First Support

Banana Cake Pop

Diagnostics
