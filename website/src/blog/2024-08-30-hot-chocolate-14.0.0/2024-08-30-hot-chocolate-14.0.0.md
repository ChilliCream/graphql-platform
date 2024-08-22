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

We are almost ready to release a new major version of Hot Chocolate and with it come so many exiting new features. We have been working on this release for quite some time and we are very excited to share it with you. In this blog post we will give you a sneak peak of what you can expect in Hot Chocolate 14.

In this post I will be focusing on Hot Chocolate server but we have also been busy working on Hot Chocolate Fusion and the Composite Schema Specification. We will be releasing more information on these projects in the coming weeks.

## Ease of use

We have been working on making Hot Chocolate easier to use and more intuitive. We have added a lot of new features that will make your life easier when working with Hot Chocolate. This will be apparent right out of the gate when you start using Hot Chocolate 14. One major area where you can see that is with dependency injection. Hot Chocolate 13 was super flexible in this are and allowed you to configure which dependencies could be used by the GraphQL execution engine with multiple resolvers at the same time and for which services the execution engine has to synchronize. This was a powerful feature but also a bit complex to use, especially when we throw DataLoader into the mix.

You either ended up with long configuration code that in essence re-declared all dependencies or you would end up with ver busy looking resolvers.

With Hot Chocolate 14 we have thrown all of this out and instead have put the DI on auto-pilot. When you write your resolvers you now can simply inject the services without telling Hot Chocolate that they are services.

```csharp
public static IQueryable<Session> GetSessions(
    ApplicationDbContext context)
    => context.Sessions.OrderBy(s => s.Title);
```

This leads to clearer code that is more understandable and easier to maintain. The above resolver for instance injects the `ApplicationDbContext`. There is no need to tell Hot Chocolate that this is a service or what characteristics this services has, it will just work.







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
