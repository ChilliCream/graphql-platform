---
path: "/blog/2020/11/23/hot-chocolate-11"
date: "2020-11-23"
title: "Welcome to the family"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "banner-hot-chocolate-11.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we are releasing Hot Chocolate server 11. We started work on this version about 1 1/2 years ago. We occasionally took a break from this project to create another 10.x version and deliver new features to the stable branch. From a user perspective, we have provided a new feature version every two months. For the core team, it was quite an intense time creating this new server and, at the same time, looking at the old version to keep it current.

With Hot Chocolate 11, we are now fully embracing .NET 5 while still supporting older .NET platforms. If you opt into .NET 5, you will get a much more refined experience to express a GraphQL schema in entirely different ways.

Records are now fully supported and let you create full GraphQL types with a single line. I personally like to use records for input types when using the pure code-first (annotation based) approach.

```csharp
public record AddSessionInput(string Title, string SpeakerId);
```

We reworked Hot Chocolate also to accept attributes on the parameters when using the short-hand syntax.

```csharp
public record AddSessionInput(string Title, [ID(nameof(Speaker))] string SpeakerId);
```

This allows you to write very slim input types and get rid of a lot of boilerplate code.

We have also started exploring how we can use source generators to make Hot Chocolate faster and even less boilerplate. You will see this trickling in with the next dot releases.

# New Configuration API

While .NET 5 support is nice, the most significant change from an API perspective is the new configuration API, which now brings together all the different builders to set up a GraphQL server into one new API.

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>();
```

The builder API lets you chain in new extension methods that can add new capabilities without the need to change the actual builder API. The actual builder interface is nothing more than a named access to the service collection, which lets you add named configurations to the DI that are consecutively used to create a GraphQL server.

```csharp
public interface IRequestExecutorBuilder
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    NameString Name { get; }

    /// <summary>
    /// Gets the application services.
    /// </summary>
    IServiceCollection Services { get; }
}
```

Significant here is our switch to allow multiple named schemas that can be hot-reloaded during runtime. This allows us to improve a lot of workloads like schema stitching. But we will have more on that later.

With the new configuration API, you now can chain in various configurations without the need to remember where these things were hidden.

```csharp
services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddType<AttendeeQueries>()
        .AddType<SessionQueries>()
        .AddType<SpeakerQueries>()
        .AddType<TrackQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddType<AttendeeMutations>()
        .AddType<SessionMutations>()
        .AddType<SpeakerMutations>()
        .AddType<TrackMutations>()
    .AddSubscriptionType(d => d.Name("Subscription"))
        .AddType<AttendeeSubscriptions>()
        .AddType<SessionSubscriptions>()
    .AddType<AttendeeType>()
    .AddType<SessionType>()
    .AddType<SpeakerType>()
    .AddType<TrackType>()
    .AddFiltering()
    .AddSorting()
    .EnableRelaySupport()
    .AddDataLoader<AttendeeByIdDataLoader>()
    .AddDataLoader<SessionByIdDataLoader>()
    .AddDataLoader<SpeakerByIdDataLoader>()
    .AddDataLoader<TrackByIdDataLoader>()
    .EnsureDatabaseIsCreated()
    .AddInMemorySubscriptions()
    .AddFileSystemQueryStorage("./persisted_queries")
    .UsePersistedQueryPipeline();
```

With the new configuration API, we also reworked the ASP.NET Core integration to use the endpoints API. It now is very easy to just apply the Hot Chocolate server to a routing configuration.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseWebSockets();
    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGraphQL();
    });
}
```

With the new middleware, we dropped support for Playground and GraphiQL and have added our own GraphQL IDE Banana Cake Pop, which will be automatically added to a GraphQL route.

![Banana Cake Pop](banana-cake-pop.png)

In order to configure Banana Cake Pop or other middleware settings you can chain in the server options with the GraphQLEndpointBuilder.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseWebSockets();
    app.UseRouting();

    app.UseEndpoints(
        e => e.MapGraphQL().WithOptions(
            new GraphQLServerOptions
            {
                Tool = { Enable = false }
            }));
}
```

# Execution Engine

While the new Configuration API is the first change, you will notice we changed a whole lot underneath. One of the biggest investments we made was into our new execution engine. The new execution engine uses a new operation optimizer component to create execution plans and optimize executing requests. The first request now is a little slower since we need to essentially compile a query and then execute it. All consecutive requests can now simply execute and no longer need to interpret things like skip, include, defer, and other things.

With the new execution engine, we also introduced a new batching mechanism that is now much more efficient and abstracts the batching mechanism from DataLoader, meaning you can write your own batching functionality and integrate it. The stitching layer, for instance, does this to batch requests to the downstream services.

Apart from this, the new DataLoader API now follows the DataLoader spec version 2 and now lets you inject the batch scheduler into the DataLoader. This makes it now easy to use GreenDonut in your business logic. The beauty of this is that you do not need to expose any GraphQL libraries into your business layer and are able to nicely layer your application.

We also rewrote the validation layer for Hot Chocolate to make it much more correct and much faster. To make the query validation more correct and ensure quality, we have ported all the `graphql-js` tests to Hot Chocolate. While porting and integrating these tests, we found countless little issues with our implementation of field merging, for instance.

So, what do we mean with much faster? We put a lot of effort in reducing our footprint.

OK, let's have a look at this compared to GraphQL .NET Server 4.3.1.

| Server           | Benchmark                  |    Time |  Allocated |
| ---------------- | -------------------------- | ------: | ---------: |
| Hot Chocolate 11 | Three Fields               |   11.94 |    7.49 KB |
| GraphQL .NET     | Three Fields               |   46.36 |   30.59 KB |
| Hot Chocolate 11 | Small Query with Fragments |   43.32 |   13.64 KB |
| GraphQL .NET     | Small Query with Fragments |  138.56 |  135.41 KB |
| Hot Chocolate 11 | Introspection              |  750.96 |  392.31 KB |
| GraphQL .NET     | Introspection              | 2277.24 | 2267.26 KB |

Hot Chocolate 11 uses a lot less memory and on top of that uses a lot less time to execute queries. But we also looked at other GraphQL servers and added Hot Chocolate to a variety of benchmarks.

For instance we ran tests against the Apollo GraphQL server.

| Server                     | Requests / second |
| -------------------------- | ----------------: |
| Hot Chocolate 11           |           19983.2 |
| graphyne                   |           17918.4 |
| express-gql                |            5931.4 |
| apollo-fastify-graphql-jit |            4046.2 |
| apollo                     |            2697.1 |

In our throughput tests, we can see that Hot Chocolate outperforms any node based GraphQL server. Hot Chocolate is optimized for parallel requests meaning the more core your system has, the better Hot Chocolate server performs. This also means that if you have, for instance, only one core graphyne will actually perform better.

This said, we are not done on performance and pulled the two biggest performance features on the execution side since we could not get them done in time for 11. We already have seen hugh potential in making the overall performance of the server faster by using source generators in order to move the resolver compiler to the build time. Also we pulled a lot of our execution plan optimizers that would rewrite the execution tree in order to optimize data fetching. These performance improvements will trickle in with the next dot releases and should push Hot Chocolate further.

# Relay

We have invested a lot of time to make it even easier to create relay schemas. One of the things I found often cumbersome was to create entities that implemented node. With Hot Chocolate 10.5 you could not do that pure code-first and always needed to use code-first with the fluent API or schema-first. This now has changed and it is much easier to write relay compliant schemas with any approach.

In order to write a node type you can just put everything into one class.

```csharp
[Node]
public class Person
{
    public int Id { get; set; }

    public string Name { get; set; }

    public static async Task<Person> GetPersonAsync(MyDbContext context, int id)
    {
        // ...
    }
}
```

But you can also split the re ....

```csharp
[Node(NodeResolverType = typeof(IPersonResolver))]
public class Person
{
    public int Id { get; set; }

    public string Name { get; set; }
}
```

The the above example the node resolver is embedded into the `Person` class

- relay nodes

# Spec

We also invested time to add more features from the

- SPEC
- interfaces implement interfaces
- Specified by on scalars
- defer

- extension API
- type interceptor
- schema interceptor
- conventions

- Data Integration
- Spatial Filtering (experimental)
- spatial types
- Outlook: MongoDB
- Outlook: Neo4J
- Entity Framework

- Schema Stitching
- hot reload
- outlook: support for af

- Strawberry Shake
