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

While .NET 5 support is nice, the most significant change from an API perspective is the new configuration API, which now brings together all the different builders to setup a GraphQL server into one new API.

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>();
```

The builder API lets you chain in new extension methods that can add new capabilities without the need of changing the actual builder API. In fact, the actual builder interface is nothing more then a named access to the service collection which lets you add named configurations to the DI that are used to create a GraphQL server.

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

Significant here is our switch to allow multiple named schemas.

EXAMPLE

- dotnet 5
- records

- new configuration API
- ASP.NET Core Routing
- bcp

- new execution engine
- new validation (graphQL-js)
- new batching / DataLoader

- relay nodes

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
