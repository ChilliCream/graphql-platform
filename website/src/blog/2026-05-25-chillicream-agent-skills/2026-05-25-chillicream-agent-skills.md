---
path: "/blog/2026/05/25/chillicream-agent-skills"
date: "2026-05-25"
title: "ChilliCream Agent Skills"
description: "We are publishing the first ChilliCream Agent Skills for GraphQL schema design, Hot Chocolate, and Green Donut so agents can follow our platform conventions instead of guessing."
tags: ["hotchocolate", "graphql", "dotnet", "ai"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
---

# ChilliCream Agent Skills

Today we are publishing the first ChilliCream Agent Skills for our platform.

Agent Skills package the instructions, conventions, and reference material an AI coding agent needs for a specific kind of work. Together with [`skillz`](/blog/2026/05/25/introducing-skillz), they make those conventions easier to install in a project and easier for an agent to load at the right time, instead of relying on a long prompt that has to be repeated for every task.

The first set combines schema design with implementation guidance: [`graphql-schema-design`](/blog/2026/05/25/introducing-skillz) focuses on the API contract, while `/graphql-backend` and `/dataloader` focus on application code. More skills will follow over the coming weeks, but this initial release is deliberately practical and aimed at the decisions that usually come up during review.

## GraphQL backend

The `/graphql-backend` skill helps agents build GraphQL APIs with Hot Chocolate v16.

It teaches the agent the shape of a modern Hot Chocolate backend:

- `[ObjectType<T>]` source-generated GraphQL types
- `[QueryType]` and `[MutationType]` root operation classes
- `[NodeResolver]` methods for Relay node refetching
- typed Relay IDs with `[ID<T>]`
- mutation conventions and generated payloads
- typed domain errors with `[Error<T>]`
- thin GraphQL wrappers over application commands and queries

The main rule is simple: the GraphQL layer stays thin. Resolver methods translate GraphQL arguments, dispatch to the application layer through the Mocha mediator, and return the result, while business logic, authorization decisions, validation, and EF Core access stay in the application layer. Because that rule is easy for an agent to violate, the skill makes it explicit.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-backend
```

## DataLoader

The `/dataloader` skill helps agents write Green Donut DataLoaders using the source generator.

DataLoaders are one of the most important performance tools in a GraphQL server, but their shape matters. A loader can compile and still be wrong if the generated interface name drifts from the convention, the return type uses `IReadOnlyDictionary`, lookups are missing, or EF Core queries forget `AsNoTracking()`.

The skill gives the agent the expected Green Donut patterns:

- `[DataLoaderGroup]` for batching contexts
- `[DataLoader]` partial method shapes
- `Dictionary<TKey, TValue>` for one-to-one loads
- `ILookup<TKey, TValue>` for one-to-many loads
- `Dictionary<TKey, Page<T>>` for paged relations
- lookup functions for the promise cache
- deterministic ordering for paged loaders
- required `CancellationToken` parameters
- `AsNoTracking()` for read-only EF Core queries

In practice, this means the agent can add a related GraphQL field and know when it should go through a DataLoader instead of reaching directly into a `DbContext`.

```bash
dnx skillz add ChilliCream/agent-skills --skill dataloader
```

## Why start here

These skills are not meant to replace documentation. They put the right documentation in front of the agent at the moment it is designing an API or writing code.

Hot Chocolate v16 introduced new source-generator patterns, a redesigned type system, better batching, and safer conventions across the server. Those changes make the platform more predictable, but they also make it more important that generated code follows the current shape. Agent Skills let us package that shape directly into the development workflow.

Install the skills with `skillz`, ask your agent to design, add, or review a GraphQL feature, and the agent can load the same rules we use when reviewing Hot Chocolate application code.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
dnx skillz add ChilliCream/agent-skills --skill graphql-backend
dnx skillz add ChilliCream/agent-skills --skill dataloader
```

If you are on .NET 8 or .NET 9, install `skillz` first:

```bash
dotnet tool install -g skillz
skillz add ChilliCream/agent-skills --skill graphql-schema-design
skillz add ChilliCream/agent-skills --skill graphql-backend
skillz add ChilliCream/agent-skills --skill dataloader
```

Over the next weeks we will publish more skills for the ChilliCream stack. The goal is not to create a pile of prompts, but to capture the conventions that make agents useful on real Hot Chocolate, Fusion, Green Donut, and Mocha codebases.
