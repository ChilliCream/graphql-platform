---
title: "Overview"
---

Hot Chocolate is an open-source [GraphQL](https://graphql.org/) server for .NET. You define your API shape using C# classes and methods, and Hot Chocolate translates that into a spec-compliant GraphQL schema. It handles parsing, validation, execution, and transport so you can focus on your domain logic.

# What Is Hot Chocolate

Hot Chocolate is a GraphQL server framework that runs on [ASP.NET Core](https://learn.microsoft.com/aspnet/core). You write C# types and resolvers. Hot Chocolate turns them into a GraphQL schema, validates incoming operations against that schema, executes them, and returns results over HTTP, WebSocket, or Server-Sent Events.

Hot Chocolate implements the [GraphQL 2025 specification](https://spec.graphql.org/) and several draft features including `@defer`, `@stream`, and `@requiresOptIn`. It implements the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) for transport. It is compatible with all spec-compliant clients, including [Strawberry Shake](/docs/strawberryshake/v16), [Relay](https://relay.dev/), and [Apollo Client](https://www.apollographql.com/docs/react/).

# How You Build a Schema

Hot Chocolate supports two approaches to building a GraphQL schema. Both produce the same result: a fully typed, spec-compliant GraphQL schema. They differ in how you express it in C#.

## Implementation-First

With implementation-first, your C# implementation is the single source of truth for your GraphQL schema. Define your API using familiar C# classes and attributes like `[QueryType]`. At build time, a source generator analyzes your code and creates the GraphQL types for you. You focus on your business logic, since your implementation is your schema. You don’t need to manually keep your code and schema in sync, deal with GraphQL-specific boilerplate, or write large type definitions in C#.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        CatalogContext db,
        CancellationToken ct)
        => await db.Products.FindAsync([id], ct);
}
```

This approach is similar to how Meta built their GraphQL server. Your schema stays close to your domain code, while the tooling handles the translation.

## Code-First

The code-first approach lets you define your GraphQL types and schema structure directly in C# using Hot Chocolate’s fluent type descriptor API.

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Field(p => p.Id)
            .Type<NonNullType<IdType>>();

        descriptor
            .Field(p => p.Name)
            .Type<NonNullType<StringType>>();
    }
}
```

Code-first is useful when you need to decouple the GraphQL schema shape from your C# model, or when you are building infrastructure that generates schemas programmatically.

Both approaches can be mixed in the same project. You can use implementation-first for most types and drop into code-first for specific cases that need more control.

# GraphQL API Security Strategies

GraphQL APIs are typically designed for one of two usage models.

Either your API is exclusively consumed by applications you control (first-party), such as your own web, mobile, or internal services, where you define and manage every client operation.

Or your API is open to external developers or partners (third-party), and you have no control over the GraphQL operations they send.

This distinction shapes how you configure Hot Chocolate and operate your GraphQL server.

## First-party GraphQL

A first-party API is consumed exclusively by your own applications. This is how Meta built and operates GraphQL internally. Because you control both the server and every client, you know every operation at build time. This enables you to maintain a precise schema usage history and strictly allow only approved GraphQL operations.

Hot Chocolate supports this scenario with **trusted documents**. You extract all operations from your client applications during their build process, register them with the server, and the server only accepts pre-registered operations.

- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) covers the full workflow: extraction, registration, and enforcement.
- [Strawberry Shake](/docs/strawberryshake/v16) and [Relay](https://relay.dev/docs/guides/persisted-queries/) both support build-time operation extraction.

When in the future you want to change or phase out parts of your schema you know the impact this change will have to your system before you apply it. This is a super power for API evolution.

## Third-party GraphQL

A third-party API is consumed by external developers or clients outside your organization. GitHub’s GraphQL API is a canonical example. You publish a schema, and external teams build applications against it. Because you do not control the clients, they can send any operation they want.

Hot Chocolate provides **cost analysis** for this scenario. You assign weights to fields and connections, and the server rejects operations that exceed the performance budget before execution begins.

- [Cost analysis](/docs/hotchocolate/v16/security/cost-analysis) explains field weights, type costs, and budget configuration.
- [Controlling introspection](/docs/hotchocolate/v16/security/introspection) lets you restrict schema visibility in production.

These two approaches complement each other. A common setup is to host both a public and an internal GraphQL API. The internal API uses trusted documents to strictly control operations, while the public API relies on cost analysis and other safeguards to manage external traffic and protect against abuse.

# Key Terminology

| Term                  | Definition                                                                                                                                                                                                                         |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Schema**            | The contract that describes what data clients can query. Hot Chocolate generates it from your C# code.                                                                                                                             |
| **Query type**        | The root type for read operations. Clients enter the graph through fields on this type.                                                                                                                                            |
| **Mutation type**     | The root type for write operations. Mutations execute serially and are expected to cause side effects.                                                                                                                             |
| **Subscription type** | The root type for real-time operations. Clients subscribe to events and receive updates as they occur.                                                                                                                             |
| **Resolver**          | A function that fetches data for a single field. In implementation-first, each public method on a `[QueryType]` class is a resolver for third-party or first-party APIs.                                                           |
| **Batch resolver**    | A resolver that fetches data for multiple parent objects in a single call, improving performance by reducing the number of backend requests. Useful for solving the N+1 problem and optimizing data access patterns.               |
| **DataLoader**        | A batching and caching layer that groups multiple individual data requests into a single batch call, eliminating the N+1 problem.                                                                                                  |
| **Source generator**  | A Roslyn source generator that inspects your C# code at build time and generates the schema registration, resolver pipelines, and DataLoader infrastructure.                                                                       |
| **Cost analysis**     | A static analysis pass that calculates the cost of a query before execution and rejects queries that exceed configured limits. Based on the [IBM Cost Analysis specification](https://ibm.github.io/graphql-specs/cost-spec.html). |
| **Trusted documents** | Pre-registered operations that the server accepts by hash. Operations not in the store are rejected. Also known as persisted operations.                                                                                           |

# Scaling Beyond a Single Server

When your API grows beyond what a single service can handle, [Fusion](/docs/fusion/v16) lets you split your schema across multiple independent services. Each service owns part of the API surface. A gateway composes them into one unified schema that clients query as a single endpoint.

Fusion is not a separate product. It builds on Hot Chocolate. A standard Hot Chocolate server can act as a Fusion subgraph without changes to its resolvers or type definitions. You can start with a single Hot Chocolate server and add Fusion later when you need independent deployment or team-level ownership boundaries.

# Next Steps

Where you go from here depends on what you need:

- **"I want to build something."** Start with the [Getting Started](/docs/hotchocolate/v16/get-started-with-graphql-in-net-core) tutorial. You will create a running GraphQL server in under five minutes.

- **"I want to understand the schema system."** Read [Defining a Schema](/docs/hotchocolate/v16/defining-a-schema). It covers queries, mutations, subscriptions, and all the GraphQL types.

- **"I need to fetch data efficiently."** Go to [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader) for batching and caching, or [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers) for the full resolver API.

- **"I need to secure my API."** See [Securing Your API](/docs/hotchocolate/v16/security) for authentication, authorization, cost analysis, and trusted documents.

- **"I'm migrating from an older version."** Read the [migration guide from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).

- **"I want to split my API across services."** See [Fusion](/docs/fusion/v16) for distributed GraphQL with a gateway.
