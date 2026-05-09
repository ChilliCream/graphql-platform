---
path: "/blog/2019/05/08/performance"
date: "2019-05-08"
title: "GraphQL - Hot Chocolate 9.0.0 - Performance Improvements"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
description: A short post to demonstrate the blog layout, table of contents, and cross-document linking.
---

A short post to demonstrate the blog layout, table of contents, and cross-document linking.

## Why GraphQL?

GraphQL gives clients the freedom to ask for exactly the data they need, no more and no less. That single property compounds into smaller payloads, fewer round trips, and a strongly typed contract between client and server.

### Compared to REST

REST endpoints expose resources at fixed shapes. GraphQL lets the consumer compose its own shape per request, which removes the long tail of "give me a thinner version of this endpoint" requests that gradually accumulate in any REST API.

### Compared to gRPC

gRPC is fantastic for service-to-service traffic with known shapes. GraphQL shines when many heterogeneous clients (web, mobile, partner integrations) all consume the same backend differently.

## Getting started with Hot Chocolate

You can spin up a Hot Chocolate server in a handful of lines.

### Project setup

Create a new ASP.NET Core project, add the `HotChocolate.AspNetCore` package, and you are ready to expose your first schema.

### Defining a query type

Start with a single class containing public methods. Each method becomes a GraphQL field, and Hot Chocolate infers the rest from the C# types.

### Wiring up the schema

Register the schema in the DI container with `AddGraphQLServer()` and map it onto your application pipeline with `MapGraphQL()`. That's the entire bootstrap.

## Where to go next

Read the [Fusion docs](../docs/fusion/index.md) for distributed schema composition, or jump straight to [Fusion sub something](../docs/fusion/sub/something.md) for the code-block showcase.

### Further reading

- The Hot Chocolate documentation index covers resolvers, types, and middleware.
- Fusion documentation is the right next step once a single schema is no longer enough.

## Closing thoughts

That's it. The header and footer are still global, the markdown above renders through the same design system the docs use, and the right-side TOC is built from these headings at compile time.
