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

We had this simple throughput test for Hot Chocolate 11, which essentially executes a simple query to fetch books and authors. Hot Chocolate 11 achieved 19.983 requests a second on our test hardware. Now, with the new execution engine, we clock in 33.702 requests a second are an additional 13.719 more requests per second with the same hardware on a test that does not even really take advantage of all the new optimizations.

Hot Chocolate 12 executes much faster but also saves on the memory used. The execution now needs only 1/3 of the memory of Hot Chocolate 11 to execute, for instance, an introspection request.

| Method                                               |      Median |     Gen 0 |    Gen 1 | Gen 2 |   Allocated |
| ---------------------------------------------------- | ----------: | --------: | -------: | ----: | ----------: |
| SchemaIntrospection 11                               |    922.4 μs |   26.3672 |   0.9766 |     - |   275.49 KB |
| SchemaIntrospection 12                               |    333.6 μs |    7.8125 |        - |     - |     84.7 KB |
| Introspection five parallel requests 11              |  4,839.8 μs |  132.8125 |   7.8125 |     - |  1377.43 KB |
| Introspection five parallel requests 12              |  1,658.6 μs |   41.0156 |        - |     - |   423.48 KB |
| Large query with data fetch 11                       | 19,322.2 μs |  312.5000 | 156.2500 |     - |  3244.58 KB |
| Large query with data fetch 12                       | 15,461.0 μs |  187.5000 |  93.7500 |     - |  1923.06 KB |
| Large query with data fetch five parallel request 11 | 38,035.6 μs | 1571.4286 | 785.7143 |     - | 16394.95 KB |
| Large query with data fetch five parallel request 12 | 26,187.5 μs |  937.5000 | 468.7500 |     - |  9613.30 KB |

We are also as always comparing to GraphQL .NET and we have to say they gained a lot of performance. Well done! When we looked the last time at GraphQL .NET they were performing very poorly with for instance a very small requests of three fields taking 31 kb to process. We did the tests against GraphQL .NET again and they were now just a little bit slower than Hot Chocolate 11 with their newest version `GraphQL.Server.Core 5.0.2`.
But Hot Chocolate 12 in the same time gained again a lot of performance.

| Method                                        |     Median | Allocated |
| --------------------------------------------- | ---------: | --------: |
| Hot Chocolate 11 Three Fields                 |   11.94 μs |      7 KB |
| Hot Chocolate 12 Three Fields                 |    9.94 μs |      3 KB |
| GraphQL .NET 4.3.1 Three Fields               |   46.36 μs |     31 KB |
| GraphQL .NET 5.0.2 Three Fields               |   22.28 μs |      8 KB |
| Hot Chocolate 11 Small Query with Fragments   |   43.32 μs |     14 KB |
| Hot Chocolate 12 Small Query with Fragments   |   21.68 μs |      7 KB |
| GraphQL .NET 4.3.1 Small Query with Fragments |  138.56 μs |    135 KB |
| GraphQL .NET 5.0.2 Small Query with Fragments |   65.83 μs |     19 KB |
| Hot Chocolate 11 Introspection                |  750.51 μs |    392 KB |
| Hot Chocolate 12 Introspection                |  262.51 μs |     67 KB |
| GraphQL .NET 4.3.1 Introspection              | 2277.24 μs |   2267 KB |
| GraphQL .NET 5.0.2 Introspection              |  676.72 μs |    169 KB |

For the introspection which produces a large query GraphQL .NET needs 2.5 times the memory of Hot Chocolate. Even if we look at the small query with just three fields will GraphQL .NET use 2.6 times more memory. The same goes for execution speed. The new execution engine is 2.2 times faster than GraphQL .NET in the test to query three fields while finishing 2.6 times faster when running a introspection query.

But to be honest we did not really use all the really nice new query plan features that we have built in with Hot Chocolate 12 in these tests, that is why we have started on a more comprehensive set of tests that use a real database and allow Hot Chocolate to use projections or even query plan batching. From Hot Chocolate 13 on we will use our new performance tests we are working on that shows more aspects of usage. We also will start including even more GraphQL frameworks like juniper, async-graphql or graphql-java.

For this blog I will stick to the once we have in our setup, so lets look at a couple more. As mentioned before we have this throughput tests which we do to see just the GraphQL overhead. It executes a simple book/author GraphQL query against in memory data. We fire those requests over HTTP post against the GraphQL servers in our tests. We start doing so with 5 users for 20 seconds, then 10 users for 20 seconds up to 30 users per seconds. We do this in a couple of rounds an let each of these run on a freshly rebooted system. We are looking at automating this with k6s and my colleague Jose will help us on that.

| Method                                     | Requests per Sek. |
| ------------------------------------------ | ----------------: |
| Hot Chocolate 12                           |            33.702 |
| Hot Chocolate 11                           |            19.983 |
| benzene-http (graphyne)                    |            17.691 |
| mercurius+graphql-jit                      |            15.185 |
| apollo-server-koa+graphql-jit+type-graphql |             4.197 |
| express-graphql                            |             3.455 |
| apollo-schema+async                        |             3.403 |
| go-graphql                                 |             2.041 |

With Hot Chocolate 13 our goal is to hit 40.000 request per seconds on the throughput tests and we are hopeful that we can achieve this with some refinements in the execution engine. Moreover, we will start investing into startup performance. We have seen in the last release notes of graphql-java that they have gained quite a lot in schema building performance and we will also start looking into this as we start our work to get Hot Chocolate into Azure Functions.

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

For this post let me show you a simple example of how you now can create dynamic types. First let me introduce a new component here called type module.

```csharp
public interface ITypeModule
{
    event EventHandler<EventArgs> TypesChanged;

    ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken);
}
```

The type modules provide types for a specific components or data sources, the new schema stitching engine for instance will use type modules to provide types to the schema.

A type module consists o an event `TypesChanged` and a method `CreateTypesAsync`. `CreateTypesAsync` is called by the schema building process to provide the types for the module. Whenever the types change of a module the module can trigger `TypesChanged` which will phase out the current schema and create a new schema instance. The Hot Chocolate server will ensure that old request are still finished of against the old schema representation while new request are already routed to the new schema with the applied type changes.

In essence `ITypeModule` will take away the complexity of providing a dynamic schema with Hot Chocolate.

But we not only introduced this new interface to provide type we also opened up our lower-level configuration API which now lets you create types in a very easy way from a Json file or what have you.

```csharp
public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
    IDescriptorContext context,
    CancellationToken cancellationToken)
{
    var types = new List<ITypeSystemMember>();

    await using var file = File.OpenRead(_file);
    using var json = await JsonDocument.ParseAsync(file, cancellationToken: cancellationToken);

    foreach (var type in json.RootElement.EnumerateArray())
    {
        var typeDefinition = new ObjectTypeDefinition(type.GetProperty("name").GetString()!);

        foreach (var field in type.GetProperty("fields").EnumerateArray())
        {
            typeDefinition.Fields.Add(
                new ObjectFieldDefinition(
                    field.GetString()!,
                    type: TypeReference.Parse("String!"),
                    pureResolver: ctx => "foo"));
        }

        types.Add(
            type.GetProperty("extension").GetBoolean()
                ? ObjectTypeExtension.CreateUnsafe(typeDefinition)
                : ObjectType.CreateUnsafe(typeDefinition));
    }

    return types;
}
```

An example of this can be found [here]() and I will also follow up this post with a details blog post on dynamic schemas that goes more into the details.

## Type Interceptors

We also added further improvements to the type initialization to allow now type interceptors to register new types during the initialization. Also on this end you are now able to hook into the type initialization analyze the types that were registered by the user and then create further types based on the initial schema. Where type modules generate type based on an external component or data source, type interceptors allow you to generate types based on types. This can be useful if you for instance create a filter API that is based on the output types provided by a user.

I will follow up this blog post also with another deep dive blog on the type system as well that goes into these topics.

# Schema-First

Another area where we have invested was schema-first. While Hot Chocolate at its very beginning was a schema-first library it developed more and more into a code-first / annotation-based library. If we look back at Hot Chocolate 11 then it almost looked like schema-first was an afterthought. With Hot Chocolate 12 we are bringing schema-first up to par with code-first and the annotation-based approach. This means that we also did some API refactoring and kicked out the old binding APIs. We did these breaking changes to align APIs.

if we now create a schema-first server it from a configuration standpoint does look very similar to code-first or annotation based services.

```csharp
using Demo.Data;
using Demo.Resolvers;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<PersonRepository>()
    .AddGraphQLServer()
    .AddDocumentFromFile("./Schema.graphql")
    .AddResolver<Query>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

There are now two things in schema first to distinguish, resolver types and runtime types. Runtime types are the representation of a GraphQL type in .NET. Meaning if we for instance look at the person type we have a .NET representation for this.

```graphql
type Person {
  name: String!
}
```

```csharp
public record Person(int Id, string Name);
```

The .NET representation in this case is a record, but it could also be a map or a Json structure. In most cases we infer the correct binding between runtime type and GraphQL type but if you need to explicitly bind we now use the same API as with the other approaches.

```csharp
builder.Services
    .AddSingleton<PersonRepository>()
    .AddGraphQLServer()
    .AddDocumentFromFile("./Schema.graphql")
    .AddResolver<Query>()
    .BindRuntimeType<Person>();
```

Or if the name does not match the .NET type name.

```csharp
builder.Services
    .AddSingleton<PersonRepository>()
    .AddGraphQLServer()
    .AddDocumentFromFile("./Schema.graphql")
    .AddResolver<Query>()
    .BindRuntimeType<Person>("Person");
```

Resolver types are .NET classes which provide resolvers, so essentially we provide a class that has a couple of methods handling data fetching. In this instance we have a type `Query` that provides a method to fetch persons.

```csharp
public class Query
{
    public IEnumerable<Person> GetPersons([Service] PersonRepository repository)
        => repository.GetPersons();
}
```

In our example the resolver type name matches the GraphQL type name, so we can infer the binding between those. If you have multiple resolver classes per GraphQL type you also can use an overload that passes in the GraphQL type name.

```csharp
.AddResolver<QueryResolvers>("Query")
```

Naturally we still have the delegate variants of the `AddResolver` configuration methods.

```csharp
.AddResolver("Query", "sayHello", ctx => "hello")
```

## Middleware and Attributes

One more thing we did is fully integrate our attributes like `UsePaging` with schema first resolvers.

Meaning you can write the following schema now:

```graphql
type Query {
  persons: [Person!]
}

type Person {
  name: String!
}
```

Then on your `Query` resolver for `persons` you can annotate it with the `UsePaging` attribute for instance. This will cause the schema initialization to rewrite the schema.

```csharp
public class Query
{
    [UsePaging]
    public IEnumerable<Person> GetPersons([Service] PersonRepository repository)
        => repository.GetPersons();
}
```

The output schema on your server would now look like:

```graphql
type Query {
  persons(
    """
    Returns the first _n_ elements from the list.
    """
    first: Int

    """
    Returns the elements in the list that come after the specified cursor.
    """
    after: String

    """
    Returns the last _n_ elements from the list.
    """
    last: Int

    """
    Returns the elements in the list that come before the specified cursor.
    """
    before: String
  ): PersonsConnection
}

type Person {
  name: String!
}

"""
A connection to a list of items.
"""
type PersonsConnection {
  """
  Information to aid in pagination.
  """
  pageInfo: PageInfo!

  """
  A list of edges.
  """
  edges: [PersonsEdge!]

  """
  A flattened list of the nodes.
  """
  nodes: [Person!]
}

"""
Information about pagination in a connection.
"""
type PageInfo {
  """
  Indicates whether more edges exist following the set defined by the clients arguments.
  """
  hasNextPage: Boolean!

  """
  Indicates whether more edges exist prior the set defined by the clients arguments.
  """
  hasPreviousPage: Boolean!

  """
  When paginating backwards, the cursor to continue.
  """
  startCursor: String

  """
  When paginating forwards, the cursor to continue.
  """
  endCursor: String
}

"""
An edge in a connection.
"""
type PersonsEdge {
  """
  A cursor for use in pagination.
  """
  cursor: String!

  """
  The item at the end of the edge.
  """
  node: Person!
}
```

So, the paging attribute rewrote the schema to now support pagination and also puts in the middleware to handle pagination. Rest assured you still can be full in control of your schema but and specify all of those types, but you also can let Hot Chocolate generate all those tedious types like paging types or filters types etc.

Going forward we are also thinking on letting descriptor attributes become directives which would allow you to annotate directly in the schema file like the following:

```graphql
type Query {
  persons: [Person!] @paging
}

type Person {
  name: String!
}
```

But for the time being schema-first really got a big update with this release and we will continue to make it better.

The schema-first demos can be found [here]().

# DataLoader

Another component that go a huge update was DataLoader. It also was one of the reasons the release candidate phase stretched so far since we had lots of issues with the changes here in user projects. First, as we already said we would do we moved all the DataLoader into the `GreenDonut` library meaning that the various DataLoader no longer are residing in `HotChocolate.Types`. Apart form that we have refactored a lot to allow DataLoader to pool more objects and also to use a unified cache. This unified cache allows better to control how much memory can be allocated by a single request and also lets us do cross DataLoader updates. Essentially, one DataLoader can now fill the cache for another DataLoader. This ofter happens when you have entities that can be looked up by multiple keys like a user that could be fetched by the name or by its id.

In order to take advantage of the new cache pass down the DataLoader options so that we can inject them from the DI.

```csharp
public class CustomBatchDataLoader : BatchDataLoader<string, string>
{
    public CustomBatchDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
        : base(batchScheduler, options)
    {
    }

    protected override Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
}
```

With DataLoader now always pass down the options and the batch scheduled and we will inject you the new unified cache. If you do not pass down the options object then we will just use a cache per DataLoader.

The DataLoader caches with Hot Chocolate 12 are also now pooled meaning that the cache object is cleaned, preserved and reused which will safe you memory. Further we have now introduced `DataLoaderDiagnosticEventListener` which allows you to monitor DataLoader execution.

All diagnostic listener can no be registered with the schema.

```csharp
services.AddGraphQLServer()
    .AddDiagnosticEventListener<MyDataLoaderEventListener>()
    ...
```

# Stream and Defer

With Hot Chocolate 11 we introduced the `@defer` directive which allows you to defer parts of your query so that you get the most important data first and the execution of more expensive parts of your query is deprioritized.
With Hot Chocolate 12 we now are introducing the `@stream` directive which allows you to take advantage of async enumerators and define how much data of a stream you want to get immediately and what shall be deferred to later point in time.

```graphql
{
  persons @stream(initialCount: 2) {
    name
  }
}
```

Stream works with any list in Hot Chocolate but it is only really efficient and gives you all the benefits of using stream if your resolver returns a `IAsyncEnumerable<T>`. Our internal paging middleware at the moment does not work with `IAsyncEnumerable<T>` which means you can stream the results but you still will have the fill execution impact on the initial piece of the query. We however will rework pagination to use `IAsyncEnumerable<T>`when slicing the data and executing the query.

Stream and defer both work with Banana Cake Pop if your browser is chrome based. We already have an update in the works to make it work with Safari. We will issue the BCP update with version 12.1.

# ASP.NET Core improvements

# The little things that will make your live easier

Apart from our big ticket items we have invested into smaller things that will help make Hot Chocolate easier to learn and make it better to use.

## Cursor Paging

For cursor paging we introduced more options that now allow you require paging boundaries like GitHub is doing with their public API.

```csharp
public class Query
{
    [UsePaging(RequirePagingBoundaries = true)] // will create MyNameConnection
    public IEnumerable<Person> GetPersons([Service] PersonRepository repository)
        => repository.GetPersons();
}
```

Further, we give you now the ability to control the connection name. The connection name now is by default inferred from the field, since this will break all existing schemas you can set a global paging option to keep the default behavior.

```csharp
services.AddGraphServerQL()
    .AddQueryType<QueryType>()
    .SetPagingOptions(new PagingOptions { InferConnectionNameFromField = false });
```

The connection name by default as I said is inferred from the field name now, this means if you have a field `friends` then the connection will be called `FriendsConnection` instead of the old behavior where we used the element type.

You also can define the connection name explicitly now.

```csharp
public class Query
{
    [UsePaging(ConnectionName = "MyName")] // will create MyNameConnection
    public IEnumerable<Person> GetPersons([Service] PersonRepository repository)
        => repository.GetPersons();
}
```

We also made it now easier to control which paging provider is used. For one you now can configure the default paging provided which if you do nothing still is the queryable paging provider.

```csharp
services.AddGraphServerQL()
    .AddQueryType<QueryType>()
    .AddCursorPagingProvider<MyCustomProvider>(defaultProvider = true);
```

Also, you now can name the provider which gives you an easy way to point to the correct paging provider for your specific case.

```csharp
services.AddGraphServerQL()
    .AddQueryType<QueryType>()
    .AddCursorPagingProvider<MyCustomProvider>("Custom");
```

```csharp
public class Query
{
    [UsePaging(ProviderName = "Custom")]
    public IEnumerable<Person> GetPersons([Service] PersonRepository repository)
        => repository.GetPersons();
}
```

Sometimes, you want to have everything in your own hands and just use Hot Chocolate to take the tedious work of generating types of your hand. In this cases you can just return the connection and we will know what to do with it.

```csharp
public class Query
{
    [UsePaging]
    public Task<Connection<Person>> GetPersons([Service] PersonRepository repository, int? first, string? after, int? last, string? before)
        => repository.GetPersonsPagedAsync(first, after, last, before);
}
```

If you are using the `HotChocolate.Data` package in combination with the connection type you even can use now our new data extensions.

```csharp
public class Query
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public Task<Connection<Person>> GetPersonsAsync(
        [Service] PersonRepository repository,
        IResolverContext context,
        CancellationToken cancellationToken)
        => repository.GetPersons()
              .Filter(Context)
              .Sort(context)
              .Project(context)
              .ApplyCursorPaginationAsync(context, cancellationToken);
}
```

## Relay

As always we are investing a lot into removing complexity from creating relay schemas. Our new version now adds the new `nodes` field which allows clients to fetch multiple nodes in one go without the need of rewriting the query since the `ids` can be passed in as variable. While the `nodes` field is not part of the relay specification it is a recommended extension.

```graphql
{
  nodes(ids: [ 1, 2 ]) {
    __typename
    ... Person {
      name
    }
    ... Cat {
      name
    }
  }
}
```

Further, we have split our `EnableRelaySupport` configuration method to allow you to opt into partial concepts of the relay specification.

```csharp
services.AddGraphServerQL()
    .AddGlobalObjectIdentification()
    .AddQueryFieldToMutationPayloads();
```

The two new configuration methods are clearer and you can now opt into the concepts you really need.

## Errors

## Middleware Validation

Validation

- Cost Complexity

Middleware

- Order Validation

AggregateError

- Enhanced error handling for variables to better pinpoint the actual error

ASP.NET Core improvements

# Banana Cake Pop

# Outlook
