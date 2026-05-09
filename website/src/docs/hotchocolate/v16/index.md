---
title: "Overview"
description: "Start here to understand what Hot Chocolate does, how a GraphQL server fits together, and where to go next in the v16 documentation."
---

Use Hot Chocolate to expose a typed GraphQL API from your .NET application. You model the API with C# types, methods, and configuration. Hot Chocolate turns that model into a spec-compliant GraphQL schema, validates incoming operations, executes resolvers, and returns results over the transport your clients use.

If your goal is "I need a GraphQL server on .NET", this page helps you choose the right next step.

```text
C# application code
        |
        v
Hot Chocolate schema
        |
        v
GraphQL endpoint
        |
        v
Web, mobile, server, and tooling clients
```

Hot Chocolate runs on [ASP.NET Core](https://learn.microsoft.com/aspnet/core), implements the [GraphQL specification](https://spec.graphql.org/), and follows the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) for HTTP transport behavior. It works with spec-compliant GraphQL clients and with the ChilliCream tooling ecosystem.

# Understand the Server in 60 Seconds

A Hot Chocolate server has a small set of moving parts. You will see these terms across the documentation.

| Part | What it does | Start here |
| --- | --- | --- |
| **Schema** | The contract clients query. It defines types, fields, arguments, mutations, subscriptions, and rules such as nullability. | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) |
| **Resolvers** | C# methods that provide values for GraphQL fields. Resolvers can call databases, services, REST APIs, files, or other data sources. | [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) |
| **Execution** | The engine validates an operation against the schema, builds an execution plan, runs resolvers, and produces a GraphQL result. | [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes) |
| **Transports** | The endpoint accepts operations over HTTP, WebSockets, and Server-Sent Events where supported by the feature you use. | [Server endpoints](/docs/hotchocolate/v16/server/endpoints) |
| **DataLoader** | Batches and caches repeated data access during one request, which helps avoid N+1 database and service calls. | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| **Nitro** | A GraphQL IDE for exploring the schema, authoring operations, and testing requests during development. | [Nitro](/docs/nitro) |

A typical request moves through the server like this:

1. A client sends a GraphQL operation to the endpoint.
2. Hot Chocolate parses and validates the operation against the schema.
3. The execution engine calls the resolvers needed by the requested fields.
4. DataLoaders batch repeated data access before the backing store is called.
5. Hot Chocolate returns a GraphQL result over HTTP, WebSockets, or Server-Sent Events.

In implementation-first code, a resolver can be as direct as a C# method:

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product GetProduct()
        => new("Hot Chocolate");
}

public record Product(string Name);
```

The `GetProduct` method becomes a `product` field on the GraphQL `Query` type. The [implementation-first guide](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first) explains when to use this approach and when to switch to code-first configuration.

# Know What Hot Chocolate Is Great At

Hot Chocolate is a good fit when you want a .NET GraphQL server that can grow from a small API to a production system with clear data fetching, security, and operations patterns.

## Hot Chocolate fits well when

- You are building a GraphQL API in ASP.NET Core.
- You want a strongly typed schema backed by C#.
- You want implementation-first development for most schema work.
- Your resolvers fetch data from EF Core, MongoDB, Marten, REST APIs, services, or custom data sources.
- You need batching and caching to reduce duplicate data access.
- You need queries, mutations, subscriptions, or incremental delivery.
- You need production controls such as authorization, cost analysis, request limits, introspection controls, observability, and trusted documents.
- You may split a larger graph across services later with Fusion.

## Choose by API shape

| If your API shape is... | Start with... | Why |
| --- | --- | --- |
| **Public GraphQL API** for external developers or partners | [Public API guide](/docs/hotchocolate/v16/guides/public-api) and [cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) | You do not control every operation clients send, so you need a published schema, clear documentation, limits, authorization, and operation cost controls. |
| **Private GraphQL API** for first-party web, mobile, or internal clients | [Private API guide](/docs/hotchocolate/v16/guides/private-api) and [trusted documents](/docs/hotchocolate/v16/performance/trusted-documents) | You control the clients, so you can register known operations and reject unknown documents at runtime. |
| **Data API** backed by databases or services | [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) | Resolvers connect GraphQL fields to your data sources. DataLoader, filtering, sorting, projections, and pagination help keep data access efficient. |
| **Realtime API** with subscriptions or streaming results | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions) and [subscriptions and realtime](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/subscriptions-and-realtime) | Subscriptions model event streams, while the server transports carry updates to clients. |
| **Distributed GraphQL API** across multiple services | [Fusion](/docs/fusion/v16) | Fusion composes multiple service contracts into one client-facing graph when a single server is no longer the right operational boundary. |

For GraphQL fundamentals, read the [GraphQL Learn](https://graphql.org/learn/) material or the [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) section before choosing detailed server configuration.

# See the Product Map

Hot Chocolate is the server runtime, but you will often use it with other ChilliCream tools.

| Area | Use it when... | Link |
| --- | --- | --- |
| **Hot Chocolate Server** | You need to build, expose, secure, test, and operate a GraphQL endpoint in .NET. | [Server docs](/docs/hotchocolate/v16/server/) |
| **GreenDonut and DataLoader** | Your graph has related entities and you need to batch or cache repeated data access during one request. | [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader) |
| **Nitro** | You want to explore a schema, write operations, inspect results, manage documents, or use ChilliCream developer tooling. | [Nitro docs](/docs/nitro) |
| **Fusion** | Multiple services or teams need to contribute to one graph without forcing clients to call many endpoints. | [Fusion v16](/docs/fusion/v16) |
| **Strawberry Shake** | You want a .NET GraphQL client with generated types for consuming a GraphQL API. | [Strawberry Shake v16](/docs/strawberryshake/v16) |

Start with a single Hot Chocolate server unless you already have a clear distributed ownership problem. Fusion is a boundary for larger systems, not a prerequisite for learning or shipping a server.

# Check Version and Support Status

You are reading the Hot Chocolate v16 documentation. The examples and links on this page target v16 APIs and package names.

Keep all Hot Chocolate packages in a project on the same major and minor version. Mixing package versions can cause restore errors, missing extension methods, or runtime behavior that does not match the documentation.

| If you are... | Go here |
| --- | --- |
| Starting a new v16 server | [Get started](/docs/hotchocolate/v16/get-started/) |
| Adding GraphQL to an existing ASP.NET Core app | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) |
| Checking required tools and package guidance | [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites) and [packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages) |
| Migrating from v15 to v16 | [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) |
| Comparing v16 concepts to earlier search results or older code | [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) and [coming-from guides](/docs/hotchocolate/v16/learn/5-coming-from/) |

Use the documentation version selector when you need another Hot Chocolate version. For release status and support windows, check the current release notes or support information for the version you deploy.

# Choose Your Recommended Path

Pick the path that matches your current goal. You do not need to read this documentation front to back.

| Goal | Start here | You will learn |
| --- | --- | --- |
| Create a new server and see a result quickly | [Get started](/docs/hotchocolate/v16/get-started/) | The shortest path to a running endpoint, Nitro, and a first operation. |
| Add GraphQL to an existing ASP.NET Core app | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) | How to register Hot Chocolate and map the `/graphql` endpoint in your current host. |
| Follow a guided learning path | [Learn Hot Chocolate](/docs/hotchocolate/v16/learn/) | The concepts and practical steps behind schema design, resolvers, data access, testing, and production readiness. |
| Build a complete tutorial server | [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) | A complete walkthrough from project setup through queries, mutations, subscriptions, DataLoader, tests, and hardening. |
| Understand the mental model before coding | [Thinking in GraphQL](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/) | How GraphQL operations, schemas, resolvers, nullability, clients, and performance relate. |
| Use reference docs after you know GraphQL | [Building a schema](/docs/hotchocolate/v16/building-a-schema/) and [Resolvers and data](/docs/hotchocolate/v16/resolvers-and-data/) | Exact pages for schema elements, resolver APIs, data middleware, and server behavior. |
| Prepare for production | [Securing your API](/docs/hotchocolate/v16/securing-your-api/) and [performance tuning](/docs/hotchocolate/v16/guides/performance) | Authentication, authorization, cost controls, request limits, trusted documents, warmup, caching, and observability. |
| Evaluate public or private API strategy | [Public API guide](/docs/hotchocolate/v16/guides/public-api) or [private API guide](/docs/hotchocolate/v16/guides/private-api) | How to choose security, document, and schema evolution controls based on who owns the clients. |

# Jump to Common Tasks

Returning users can use these shortcuts.

## Build the schema

- [Define queries](/docs/hotchocolate/v16/building-a-schema/queries)
- [Define mutations](/docs/hotchocolate/v16/building-a-schema/mutations)
- [Define subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions)
- [Model object types](/docs/hotchocolate/v16/building-a-schema/object-types)
- [Add arguments](/docs/hotchocolate/v16/building-a-schema/arguments)
- [Use interfaces and unions](/docs/hotchocolate/v16/building-a-schema/interfaces)
- [Document the schema](/docs/hotchocolate/v16/building-a-schema/documentation)
- [Plan schema evolution](/docs/hotchocolate/v16/building-a-schema/versioning)

## Fetch data

- [Write resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers)
- [Use dependency injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection)
- [Batch with DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader)
- [Fetch from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases)
- [Fetch from REST APIs](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest)
- [Add pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)
- [Add filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering)
- [Add sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting)
- [Add projections](/docs/hotchocolate/v16/resolvers-and-data/projections)

## Expose the server

- [Configure endpoints](/docs/hotchocolate/v16/server/endpoints)
- [Use HTTP transport](/docs/hotchocolate/v16/server/http-transport)
- [Handle files](/docs/hotchocolate/v16/server/files)
- [Configure interceptors](/docs/hotchocolate/v16/server/interceptors)
- [Use global state](/docs/hotchocolate/v16/server/global-state)
- [Export schemas with the command line](/docs/hotchocolate/v16/server/command-line)

## Secure and harden

- [Configure authentication](/docs/hotchocolate/v16/securing-your-api/authentication)
- [Configure authorization](/docs/hotchocolate/v16/securing-your-api/authorization)
- [Set request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)
- [Use cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)
- [Control introspection](/docs/hotchocolate/v16/securing-your-api/introspection)
- [Use trusted documents](/docs/hotchocolate/v16/performance/trusted-documents)
- [Use automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)

## Test and operate

- [Test GraphQL APIs](/docs/hotchocolate/v16/guides/testing)
- [Use server warmup](/docs/hotchocolate/v16/server/warmup)
- [Tune performance](/docs/hotchocolate/v16/guides/performance)
- [Add instrumentation](/docs/hotchocolate/v16/server/instrumentation)
- [Configure cache control](/docs/hotchocolate/v16/server/cache-control)
- [Understand the execution engine](/docs/hotchocolate/v16/execution-engine/)

## Migrate or compare

- [Migration guides](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16)
- [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/)
- [Coming from REST controllers](/docs/hotchocolate/v16/learn/5-coming-from/rest-controllers)
- [Coming from GraphQL.NET](/docs/hotchocolate/v16/learn/5-coming-from/graphql-dotnet)

# When You Get Stuck

Use the symptom that matches what you see.

| Symptom | Best next page | Expected recovery |
| --- | --- | --- |
| You do not know whether to start with Get Started or Learn. | [Get started](/docs/hotchocolate/v16/get-started/) or [Learn](/docs/hotchocolate/v16/learn/) | Choose fast setup for first success, or the guided learning path for deeper understanding. |
| You already have an ASP.NET Core app. | [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app) or [existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app) | Register Hot Chocolate in your existing service collection and map the GraphQL endpoint. |
| You are unsure whether to use implementation-first or code-first. | [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first) | Use implementation-first as the default, then choose code-first when you need descriptor-level control. |
| Nitro does not appear or you do not know the endpoint path. | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) and [endpoints](/docs/hotchocolate/v16/server/endpoints) | Confirm the server is running, the endpoint is mapped, and you are opening the correct URL. |
| Your schema does not contain the field you wrote. | [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting), [queries](/docs/hotchocolate/v16/building-a-schema/queries), and [object types](/docs/hotchocolate/v16/building-a-schema/object-types) | Register the type, check method visibility and naming, and verify the field in Nitro or exported SDL. |
| Package restore or runtime errors mention missing methods or type mismatches. | [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites) and [packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages) | Align all Hot Chocolate package versions and restore again. |

# Next Steps

If you are new, start with [Get started](/docs/hotchocolate/v16/get-started/). If you want the deeper path, go to [Learn Hot Chocolate](/docs/hotchocolate/v16/learn/). If you already have a server, use the task links above to jump directly to schema, data fetching, server configuration, security, testing, or production guidance.
