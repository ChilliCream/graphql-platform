---
path: "/blog/2024/08/30/new-in-hot-chocolate-13"
date: "2023-02-08"
title: "What's new for Hot Chocolate 13"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "hot-chocolate-13-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

We are almost ready to release a new major version of Hot Chocolate, and with it come many exciting new features. We have been working on this release for quite some time, and we are thrilled to share it with you. In this blog post, we will give you a sneak peek at what you can expect with Hot Chocolate 14.

In this post, I will be focusing on the Hot Chocolate server, but we have also been busy working on Hot Chocolate Fusion and the Composite Schema Specification. We will be releasing more information on these projects in the coming weeks.

## Ease of use

We have focused on making Hot Chocolate easier to use and more intuitive. To achieve this, we have added many new features that will simplify your work. This will be apparent right from the start when you begin using Hot Chocolate 14. One major area where you can see this improvement is in dependency injection. Hot Chocolate 13 was incredibly flexible in this area, allowing you to configure which dependencies could be used by the GraphQL execution engine with multiple resolvers simultaneously, and to specify which services the execution engine needed to synchronize or pool. While this was a powerful feature, it could be somewhat complex to use, especially when incorporating DataLoader into the mix.

You either ended up with lengthy configuration code that essentially re-declared all dependencies, or you ended up with very cluttered resolvers.

With Hot Chocolate 14, we have simplified this process by putting dependency injection on auto-pilot. Now, when you write your resolvers, you can simply inject services without needing to explicitly tell Hot Chocolate that they are services.

```csharp
public static IQueryable<Session> GetSessions(
    ApplicationDbContext context)
    => context.Sessions.OrderBy(s => s.Title);
```

This leads to clearer code that is more understandable and easier to maintain. The above resolver for instance injects the `ApplicationDbContext`. There is no need to tell Hot Chocolate that this is a service or what characteristics this services has, it will just work. This is because we have simplified the way Hot Chocolate interacts with the dependency injection system.

In GraphQL we in essence have two execution algorithms, the first one that is used for queries allows for parallelization to optimize data fetching. This allows us to enqueue transparently data fetching requests and execute them in parallel. The second algorithm is used for mutations and is a sequential algorithm that executes one mutation after the other.

So, how is this related to DI? In Hot Chocolate 14 if we have an async resolver that requires services from the DI we will create a service scope around it. Ensuring that the service you use in the resolver are not used by other resolvers concurrently. Since query resolvers are per spec defined as side-effect free this is a great default behavior.

For mutations this is very different, as we are cause with the mutation a side-effect and you might want to use for instance a shared DBContext between two mutations, or y






When I talk about ease of use here I am also including security into that. GraphQL security was a major struggle for many developers and with vers


Security

Ease of use

Dependency Injection

Layering

Pagination

DataLoader

Fusion

Composite Schema Specification

Community

Source Generators

IsSelected
