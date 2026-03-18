---
title: "Overview"
---

Hot Chocolate is an open-source [GraphQL](https://graphql.org/) server for .NET. You define your API shape using C# classes and methods, and Hot Chocolate translates that into a spec-compliant GraphQL schema. It handles parsing, validation, execution, and transport so you can focus on your domain logic.

# What Is Hot Chocolate

Hot Chocolate is a GraphQL server framework that runs on [ASP.NET Core](https://learn.microsoft.com/aspnet/core). You write C# types and resolvers. Hot Chocolate turns them into a GraphQL schema, validates incoming operations against that schema, executes them, and returns results over HTTP, WebSocket, or Server-Sent Events.

Hot Chocolate implements the [GraphQL 2025 specification](https://spec.graphql.org/) and several draft features including `@defer`, `@stream`, and `@requiresOptIn`. It implements the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) for transport. It is compatible with all spec-compliant clients, including [Strawberry Shake](/docs/strawberryshake/v16), [Relay](https://relay.dev/), and [Apollo Client](https://www.apollographql.com/docs/react/).

# How You Build a Schema

Hot Chocolate supports two approaches to building a GraphQL schema. Both produce the same result: a fully typed, spec-compliant GraphQL schema. They differ in how you express it in C#.

## Implementation-first (recommended)

You write standard C# classes and decorate them with attributes like `[QueryType]`. A source generator inspects your code at build time and produces the GraphQL schema automatically. This is the recommended approach and the one used throughout this documentation.

```csharp
// Types/ProductQueries.cs
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

The source generator creates a `book` field on the Query type, infers argument types from the method parameters, and registers everything with the schema. You do not write GraphQL SDL or configure type descriptors.

This approach matches how Meta originally built GraphQL and how many large-scale GraphQL servers are built today. It keeps your schema definition close to your domain code and lets the tooling handle the translation.

## Code-first

You create classes that inherit from `ObjectType<T>`, `InputObjectType<T>`, and other base types. You configure each type explicitly using a descriptor API. This approach gives you full control over every aspect of the schema.

```csharp
// Types/ProductType.cs
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

# Public and Private GraphQL

Most GraphQL APIs fall into one of two categories, and the choice shapes how you configure Hot Chocolate.

## Public GraphQL

A public API is consumed by third-party developers or external clients. GitHub's GraphQL API is the canonical example. You publish a schema, and external teams build applications against it. Because you do not control the clients, they can send any operation they want.

Hot Chocolate provides **cost analysis** for this scenario. You assign weights to fields and connections, and the server rejects operations that exceed the budget before execution begins.

- [Cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) explains field weights, type costs, and budget configuration.
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) limits access to types and fields based on roles or policies.
- [Controlling introspection](/docs/hotchocolate/v16/securing-your-api/introspection) lets you restrict schema visibility in production.

## Private GraphQL

A private API is consumed by your own applications. This is how Meta built and operates GraphQL internally. You control both the server and every client. You know every operation at build time.

Hot Chocolate provides **trusted documents** for this scenario. You extract all operations from your client applications during their build process, register them with the server, and the server only accepts pre-registered operations.

- [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) covers the full workflow: extraction, registration, and enforcement.
- [Strawberry Shake](/docs/strawberryshake/v16) and [Relay](https://relay.dev/docs/guides/persisted-queries/) both support build-time operation extraction.

These two approaches complement each other. A common setup is trusted documents for your own frontend applications and cost analysis for partner integrations.

# Key Terminology

| Term                  | Definition                                                                                                                                                                                                                         |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Schema**            | The contract that describes what data clients can query. Hot Chocolate generates it from your C# code.                                                                                                                             |
| **Query type**        | The root type for read operations. Clients enter the graph through fields on this type.                                                                                                                                            |
| **Mutation type**     | The root type for write operations. Mutations execute serially and are expected to cause side effects.                                                                                                                             |
| **Subscription type** | The root type for real-time operations. Clients subscribe to events and receive updates as they occur.                                                                                                                             |
| **Resolver**          | A function that fetches data for a single field. In implementation-first, each public method on a `[QueryType]` class is a resolver.                                                                                               |
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

- **"I need to fetch data efficiently."** Go to [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) for batching and caching, or [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for the full resolver API.

- **"I need to secure my API."** See [Securing Your API](/docs/hotchocolate/v16/security) for authentication, authorization, cost analysis, and trusted documents.

- **"I'm migrating from an older version."** Read the [migration guide from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).

- **"I want to split my API across services."** See [Fusion](/docs/fusion/v16) for distributed GraphQL with a gateway.
