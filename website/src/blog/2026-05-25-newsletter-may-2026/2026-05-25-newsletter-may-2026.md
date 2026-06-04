---
path: "/blog/2026/05/25/newsletter-may-2026"
date: "2026-05-25"
title: "Newsletter May 2026"
description: "May 2026 brings Hot Chocolate 16, Fusion 16, ChilliCream Agent Skills, and GraphQLConf 2026."
tags: ["hotchocolate", "fusion", "graphql", "dotnet", "ai", "release"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
---

# Newsletter May 2026

It has been more than a year since our last major platform cycle, and May has been a big month for us.

Hot Chocolate 16 and Fusion 16 are out, `dnx skillz` is available on NuGet, we have started publishing skills for our stack, and GraphQLConf 2026 brought the GraphQL community together in the Bay Area.

Here is what we shipped and what comes next.

## Hot Chocolate 16

Hot Chocolate 16 is our first new major release of the platform in more than a year, and it touches some of the deepest parts of the server. We reworked the type system, tightened scalar contracts, improved batching, adopted new GraphQL proposals, and made the defaults safer.

The new type system is the foundation for a lot of what comes next. It gives Hot Chocolate, the mutable SDL model, and Fusion a shared abstraction, which reduces duplication and makes cross-cutting features easier to build.

Read the full post: [What's new for Hot Chocolate 16](/blog/2026/05/11/hot-chocolate-16).

## Fusion 16

Fusion 16 is a major step for our distributed GraphQL work. It is no longer built as an extension on top of Hot Chocolate; instead, it now has its own architecture while staying fully aligned with ASP.NET Core and the .NET ecosystem.

That gives Fusion a cleaner foundation for gateway execution, planning, and composition. Your gateway remains an ASP.NET Core application, so authentication, configuration, resilience, and observability stay in your hands.

Read the full post: [What's new in Fusion 16](/blog/2026/05/15/fusion-16).

## `skillz` and agent skills

We are also starting to publish Agent Skills for the ChilliCream platform. They let us package our conventions as reusable instructions for AI coding agents, so instead of giving the agent a long prompt every time, you install a skill once and let the agent load it when a matching task comes up.

We are starting with practical skills for schema design and application code:

- `/graphql-schema-design`, for GraphQL schema design and review
- `/graphql-backend`, for Hot Chocolate v16 GraphQL backend patterns
- `/dataloader`, for Green Donut DataLoaders using the source generator

The schema design skill helps agents ask the right client-first questions, propose SDL, and review nullability, naming, pagination, errors, and schema evolution before implementation starts.

The GraphQL backend skill helps agents keep GraphQL resolvers thin, use `[ObjectType<T>]`, `[QueryType]`, `[MutationType]`, `[NodeResolver]`, typed IDs, mutation conventions, and typed errors correctly.

The DataLoader skill helps agents write DataLoaders that follow the Green Donut source-generator shape, including batching contexts, lookup functions, `Dictionary<TKey, TValue>` returns, `ILookup<TKey, TValue>` relations, paged loaders, deterministic ordering, cancellation, and `AsNoTracking()`.

Install them with `skillz`:

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
dnx skillz add ChilliCream/agent-skills --skill graphql-backend
dnx skillz add ChilliCream/agent-skills --skill dataloader
```

Read the announcement: [ChilliCream Agent Skills](/blog/2026/05/25/chillicream-agent-skills).

## GraphQLConf 2026

GraphQLConf 2026 took place May 19-20 in Fremont, California, followed by Working Group Day on May 21 at Meta's Fremont campus. This year's conference was closely aligned with the themes we have been working on, including federation, schema evolution, GraphQL for agents, and the next round of GraphQL proposals.

ChilliCream was part of the program with sessions around Semantic Introspection, federation, and GraphQL for agents.

If you missed the context, start with our Semantic Introspection post: [Semantic Introspection](/blog/2026/04/22/semantic-introspection).

## Thank you

Thank you to everyone testing the previews, filing issues, discussing schema design with us, and pushing GraphQL forward.

May was a release month, but the work continues. We will keep publishing more Agent Skills, more v16 documentation, and more guidance for building production GraphQL systems on .NET.
