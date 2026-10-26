---
title: "GraphQL Federation vs. Schema Stitching"
metaTitle: "GraphQL Federation vs Schema Stitching: Key Differences"
description: "Compare GraphQL Federation and Schema Stitching. Discover how they differ in team isolation, schema composition, distributed execution, and API ownership."
date: "2026-10-26"
tags: ["graphql-federation", "comparison", "schema-stitching"]
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

GraphQL Federation and Schema Stitching both allow multiple GraphQL services to be combined into one unified API. Clients interact with a single GraphQL schema, while requests are executed across the services behind it.

However, the two approaches address the problem from different architectural perspectives.

Schema Stitching focuses on integrating existing GraphQL APIs, often through a shared GraphQL server that manages schema transformations, relationships, and query delegation.

GraphQL Federation focuses on independent ownership and isolation. Each team owns its subgraph, develops and deploys it independently, and defines how it contributes to the larger graph. Schema composition validates those contributions, while a dedicated gateway handles distributed query execution.

**The fundamental difference is that Schema Stitching brings APIs together, while GraphQL Federation allows teams to work independently without fragmenting the API their clients consume.**

# What is GraphQL Schema Stitching?

Schema Stitching is a technique for combining multiple GraphQL schemas into a single executable schema.

A stitching server sits in front of other GraphQL services and exposes their types and fields through one API. Developers can merge types, transform schemas, and establish relationships between services.

For example, an application might have a catalog service providing product information and an inventory service exposing stock availability. Schema Stitching allows these services to contribute to one GraphQL API, so clients can query product information and availability together.

Traditionally, developers configure how schemas are merged and how queries are delegated between services. This may involve writing custom resolvers, defining schema transformations, and maintaining relationships within the stitching layer.

Modern stitching libraries, such as GraphQL Tools, simplify this process through automated type merging, query planning, batching, and declarative stitching directives.

This makes Schema Stitching particularly useful when integrating existing GraphQL APIs, especially when those services were not designed to participate in a shared graph.

# The challenges of Schema Stitching at scale

Schema Stitching can be an effective way to combine a small number of GraphQL services. However, as the number of services and teams grows, the integration layer can become increasingly complex.

## Maintaining the integration layer

In traditional stitching architectures, relationships between services are often defined through configuration or custom application code maintained by the team responsible for the stitching server.

When a service changes its schema, the shared integration layer may also need to change.

As more services participate, this can introduce coordination between service teams and the team maintaining the unified API.

Modern Schema Stitching supports declarative relationships defined within service schemas, reducing the need for centralized integration code. However, the architecture still depends on how teams organize ownership of these relationships and the stitching configuration.

## Operating a shared GraphQL server

A stitching server is another GraphQL application that must be maintained, deployed, scaled, and monitored.

It becomes a critical component because clients depend on it to access the underlying services.

If this server becomes unavailable, the unified API becomes unavailable as well.

This isn't unique to Schema Stitching. GraphQL federation gateways also require appropriate redundancy, scaling, and operational monitoring.

The difference is that federation uses a dedicated gateway designed for distributed execution, rather than requiring teams to build and maintain their own integration application.

## Coordinating changes across teams

When relationships and schema transformations are maintained centrally, changes to individual services may require corresponding updates to the stitching layer.

This can introduce an additional coordination point between teams.

Schema Stitching can mitigate this through declarative directives, automated validation, and independent deployment pipelines.

GraphQL Federation makes independent ownership and schema composition central parts of its architectural model.

# How GraphQL Federation approaches the problem

GraphQL Federation allows independently developed services, called subgraphs, to contribute to one unified GraphQL API.

Each subgraph publishes a source schema describing the types and fields it owns, including how those types relate to other services.

A process called schema composition validates these source schemas and produces one composite schema.

A GraphQL gateway then executes client queries across the participating subgraphs and assembles their results into a single response.

Instead of maintaining relationships between services through custom integration logic in a central application, federation allows teams to declare those relationships in their own schemas.

For example, a catalog service can define a product and expose a lookup for retrieving it:

```graphql
# Catalog subgraph

type Query {
  productById(id: ID!): Product @lookup
}

type Product @key(fields: "id") {
  id: ID!
  name: String!
}
```

An inventory service can contribute availability information to the same product:

```graphql
# Inventory subgraph

type Query {
  productById(id: ID!): Product @lookup @internal
}

type Product @key(fields: "id") {
  id: ID!
  inStock: Boolean!
}
```

Both services independently define their contributions to the `Product` type.

The `@key` directive establishes the identity of the product, while `@lookup` describes how the distributed executor can retrieve it.

Schema composition validates these definitions and combines them into one graph.

Clients can then request product information and availability through a single GraphQL query without needing to know which service provides each field.

# Independent teams, isolated development

The defining advantage of GraphQL Federation is that teams can develop, test, and deploy their services independently.

Each team owns its subgraph, including its schema, implementation, and deployment lifecycle.

A team can introduce new functionality without modifying the codebase or deployment pipeline of another service. Dependencies between services are expressed through GraphQL contracts rather than maintained as custom integration logic within a central application.

Schema composition validates how independently developed schemas fit together. This allows incompatible changes to be identified before a new composite schema is published.

Teams can therefore evolve their services independently while maintaining a shared API contract.

This approach aligns naturally with Domain-Driven Design, where bounded contexts establish clear ownership and responsibilities within a larger system.

Each subgraph can represent a distinct business domain, exposing its capabilities while participating in a unified graph.

These boundaries also become valuable when working with AI coding agents. A well-defined subgraph gives an agent a focused context, explicit responsibilities, and a clear API contract against which changes can be validated.

Schema Stitching can support independently deployed services as well. However, federation makes independent ownership a fundamental architectural principle rather than something teams must establish around the integration layer.

**The goal is not merely to distribute execution. It's to enable independent development without sacrificing a unified API.**

# A dedicated gateway for distributed execution

GraphQL Federation separates schema composition from distributed query execution.

Source schemas are validated and combined into a composite schema, which the gateway uses to execute queries across subgraphs.

This allows federation gateways to focus on the requirements of distributed GraphQL execution.

Depending on the implementation, these capabilities can include:

- Query planning across multiple subgraphs.
- Operation plan caching and optimization.
- Batching and deduplication of downstream requests.
- Distributed tracing and observability.
- Centralized security and authorization controls.

Modern Schema Stitching can provide many of these capabilities as well, including automated delegation planning and batching.

The distinction is therefore not that Schema Stitching cannot perform distributed execution efficiently.

Rather, federation establishes a shared architectural contract between independently developed services and a gateway responsible for executing the composed graph.

For organizations with many independent teams, this separation can reduce the amount of custom integration infrastructure they need to develop and maintain.

# GraphQL Federation vs. Schema Stitching at a glance

| Capability                  | Schema Stitching                              | GraphQL Federation                                |
| --------------------------- | --------------------------------------------- | ------------------------------------------------- |
| Primary focus               | Integrating GraphQL APIs                      | Independent service ownership and isolation       |
| Unified GraphQL API         | Supported                                     | Supported                                         |
| Architecture                | Executable stitched schema                    | Composite schema and federation gateway           |
| Service ownership           | Depends on architecture                       | Explicit subgraph ownership                       |
| Cross-service relationships | Gateway configuration or stitching directives | Federation directives in source schemas           |
| Custom integration code     | Sometimes required                            | Typically less gateway-specific code              |
| Schema composition          | Stitching and merging configuration           | Specification-defined composition                 |
| Independent deployments     | Supported                                     | Core design principle                             |
| Distributed execution       | Schema delegation and query planning          | Federation gateway                                |
| Schema transformations      | Extensive support                             | Defined by composition and implementation tooling |
| Horizontal scaling          | Supported                                     | Supported                                         |

Both approaches can build unified GraphQL APIs across distributed services.

The most important difference is how they establish and maintain ownership boundaries as the architecture grows.

# When should you use Schema Stitching?

Schema Stitching is particularly useful when combining existing GraphQL APIs that were not designed to work together.

Its schema transformation capabilities make it well suited to integrating third-party APIs, adapting existing schemas, or building an aggregation layer without modifying the underlying services.

For smaller integrations, Schema Stitching can also be a straightforward solution when independent subgraph ownership and standardized composition are not primary requirements.

# When should you use GraphQL Federation?

GraphQL Federation is particularly well suited to larger APIs developed by multiple teams that need independent ownership and deployment.

Each team can evolve its subgraph without coordinating implementation changes or deployments across the entire organization.

Schema composition validates how those independently developed services contribute to the overall graph, while the gateway handles distributed execution.

This makes federation a natural choice when organizations want to organize their APIs around clear domain boundaries, distribute development across teams, and preserve one coherent API for clients.

**Schema Stitching is primarily about integrating APIs. GraphQL Federation is about enabling independent teams to build one API together.**

# Build distributed GraphQL APIs with Fusion

Fusion is a high-performance GraphQL gateway designed for independently developed services.

It supports both the GraphQL Federation specification and Apollo Federation, allowing teams to preserve their existing subgraphs while adopting the open specification incrementally.

Fusion also connects GraphQL, REST, and gRPC services, enabling organizations to build unified APIs across different technologies without maintaining a custom stitching server.

With build-time schema composition, distributed query execution, and integrated performance and security capabilities, Fusion provides the infrastructure for operating federated GraphQL APIs at scale.

[Explore Fusion](/products/fusion) | [View Benchmarks](/benchmarks/graphql-federation)
