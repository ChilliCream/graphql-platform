---
title: "GraphQL Federation vs. Apollo Federation"
metaTitle: "GraphQL Federation vs Apollo Federation: Key Differences"
description: "Learn how GraphQL Federation evolves Apollo Federation into an open standard. Explore technical differences, backward compatibility, and Fusion support."
featuredImage: "graphql-federation-vs-apollo-federation.png"
date: "2026-10-26"
tags: ["graphql-federation", "comparison", "apollo-federation"]
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

Apollo Federation established the foundation for building distributed GraphQL APIs. GraphQL Federation takes that architecture forward, combining ideas from Apollo Federation and GraphQL Fusion into an open, vendor-neutral specification developed under the GraphQL Foundation.

**GraphQL Federation is an evolution of Apollo Federation, not a replacement.** It preserves the fundamental architecture of independent subgraphs, schema composition, and distributed query execution while simplifying how GraphQL services participate in a federated graph.

Backward compatibility is an important part of this evolution. Existing Apollo Federation subgraphs can continue operating without modification through gateways that support both approaches. Fusion already provides this interoperability, allowing Apollo Federation and GraphQL Federation subgraphs to participate in the same graph.

Meanwhile, Apollo, ChilliCream, The Guild, WunderGraph, and other contributors are helping advance the new specification, creating a shared foundation for the future of GraphQL federation.

# From Apollo Federation to an open standard

Apollo introduced federation in 2019, allowing organizations to distribute ownership of a GraphQL API across independent teams and services. Instead of maintaining one large GraphQL server, teams could develop and deploy their own subgraphs while exposing a single unified API to clients.

GraphQL Fusion subsequently explored another approach to federation, emphasizing the existing GraphQL type system, explicit schema relationships, and reducing the need for specialized subgraph protocols.

Both approaches addressed the same architectural challenge but made different technical choices. A subgraph designed for one federation system could not necessarily participate in another without additional integration.

In 2023, Apollo, ChilliCream, and The Guild established the Composite Schemas Working Group to develop a common specification under the GraphQL Foundation. The effort has since brought together contributors from across the GraphQL ecosystem, including IBM, Netflix, WunderGraph and the broader GraphQL community.

What began as the Composite Schemas specification has now become the **GraphQL Federation specification**, bringing together the experience gained from both Apollo Federation and GraphQL Fusion.

The goal is a shared standard that different GraphQL servers, gateways, and tooling providers can implement independently, without tying federation to a single vendor.

# What stays the same?

GraphQL Federation preserves the fundamental architectural concepts that made Apollo Federation successful.

- **Subgraphs:** Independent GraphQL services contribute types and fields to a larger graph.
- **Entities and keys:** Types can be identified and referenced across service boundaries.
- **Schema composition:** Source schemas are validated and combined into one unified schema.
- **Distributed execution:** A gateway coordinates queries across the necessary subgraphs and assembles a single response.

The terminology has evolved slightly. What Apollo Federation calls a _supergraph_, GraphQL Federation calls a _composite schema_. Apollo commonly uses the term _router_, while the open specification uses _gateway_.

But the underlying architecture remains familiar.

Each team owns its services, schemas, and deployment lifecycle. Clients interact with one coherent GraphQL API without needing to know which subgraph provides each field.

# What changes with GraphQL Federation?

The most significant changes concern how subgraphs expose entities, declare dependencies, and communicate with the gateway.

Apollo Federation introduced a dedicated subgraph protocol to support distributed execution. GraphQL Federation builds on standard GraphQL capabilities instead, making federation possible without requiring every server to implement specialized federation infrastructure.

## Entity resolution through standard GraphQL fields

Apollo Federation resolves entities through a special `_entities` field. Subgraphs implement reference resolvers that accept entity representations containing the information needed to locate an entity.

GraphQL Federation introduces a more explicit approach using ordinary GraphQL query fields marked with `@lookup`.

```graphql
type Query {
  productById(id: ID!): Product @lookup
}

type Product @key(fields: "id") {
  id: ID!
  name: String!
}
```

The `@key` directive identifies an entity, while `@lookup` tells the distributed executor how that entity can be retrieved.

Unlike Apollo's specialized `_entities` field, `productById` is a regular GraphQL query field. Developers can query it directly, test it independently, and understand its purpose without knowing anything about federation.

An entity can also expose multiple lookups, allowing it to be retrieved through different keys.

This approach eliminates the need for dedicated reference resolvers and allows existing GraphQL query fields to participate directly in federation.

## Dependencies through field arguments

Federated queries often require data owned by one subgraph to resolve a field in another.

Apollo Federation expresses these dependencies using directives such as `@requires`, with the required information supplied through entity representations.

GraphQL Federation introduces `@require`, which declares dependencies directly on field arguments.

Consider a shipping service that calculates delivery costs using product information owned by another service.

```graphql
type Product @key(fields: "id") {
  id: ID!
  estimateShippingCost(
    zipCode: String!
    dimensions: DimensionsInput!
      @require(
        field: """
        {
          width: dimensions.width
          height: dimensions.height
          weight
        }
        """
      )
  ): Float
}

input DimensionsInput {
  width: Int!
  height: Int!
  weight: Int!
}
```

The shipping service declares exactly which information it needs. The distributed executor retrieves that information from the appropriate subgraph and passes it through an ordinary GraphQL argument.

The service receives a strongly typed input object rather than a specialized federation representation.

This keeps dependencies explicit, allows composition to validate them, and makes the resolver easier to understand and test.

## Batching as a transport capability

Apollo Federation uses the `_entities` field to resolve multiple entity representations within a single request.

GraphQL Federation resolves entities through ordinary query fields. To support efficient execution when many entities must be fetched, the standardization effort is also introducing additional GraphQL-over-HTTP transport capabilities.

One of these is variable batching, which allows the same GraphQL operation to execute with multiple sets of variables.

```json
{
  "query": "query GetProduct($id: ID!) { productById(id: $id) { name } }",
  "variables": [{ "id": "1" }, { "id": "2" }, { "id": "3" }]
}
```

Instead of constructing different operations for different batch sizes, the gateway can reuse the same query with multiple variable sets.

The important architectural distinction is that batching becomes a transport capability rather than something built into a specialized entity-resolution protocol.

These capabilities can also benefit ordinary GraphQL clients, extending their usefulness beyond federation.

# Technical differences at a glance

| Capability           | Apollo Federation                   | GraphQL Federation                      |
| -------------------- | ----------------------------------- | --------------------------------------- |
| Architecture         | Independent subgraphs               | Independent subgraphs                   |
| Unified schema       | Supergraph                          | Composite schema                        |
| Public entry point   | Router                              | Gateway                                 |
| Schema composition   | Supported                           | Supported                               |
| Entity identity      | `@key`                              | `@key`                                  |
| Entity resolution    | `_entities` and reference resolvers | Standard query fields with `@lookup`    |
| Field dependencies   | `@requires` on fields               | `@require` on arguments                 |
| Subgraph integration | Apollo Federation subgraph protocol | Standard GraphQL schema and execution   |
| Entity batching      | Entity representations              | GraphQL-over-HTTP batching capabilities |

Some differences are primarily terminology. Others represent meaningful changes to how services participate in federation.

The underlying architectural principles remain consistent.

# Backward compatibility: Keep the subgraphs you already have

Moving toward GraphQL Federation does not have to mean rewriting existing Apollo Federation services.

**Fusion already supports both Apollo Federation and GraphQL Federation within the same graph.**

Existing Apollo Federation subgraphs can retain their schemas, reference resolvers, and federation protocols. New subgraphs can adopt the GraphQL Federation specification, and both can participate in the same composite schema.

At composition time, Fusion recognizes the federation model used by each subgraph and combines their schemas into one graph.

At runtime, Fusion communicates with each subgraph using its existing protocol. Apollo Federation subgraphs continue using `_entities`, while GraphQL Federation subgraphs use ordinary lookup fields.

Both can participate in the same distributed query execution plan, allowing a single client query to move between subgraphs using different federation protocols.

This means organizations can adopt the new specification incrementally, without a coordinated migration or disruption to existing services.

Teams can preserve their investment in Apollo Federation while introducing new subgraphs using the open specification.

**Keep the graph you have. Build the graph you want.**

# One federation ecosystem, built together

GraphQL Federation represents the next chapter of an architecture that Apollo introduced in 2019.

By bringing together the experience of Apollo Federation and ChilliCream's GraphQL Fusion, the GraphQL community is developing a shared specification that preserves existing architectural investments while opening federation to a broader ecosystem of implementations.

Apollo, ChilliCream, The Guild, WunderGraph, and other contributors are actively shaping this direction through the GraphQL Foundation.

The specification is still evolving, but implementations are already bringing its ideas into production. Fusion supports both federation models today, demonstrating how existing Apollo Federation subgraphs and services adopting GraphQL Federation can work together in one graph.

The future of federation is **not** about choosing between Apollo Federation and GraphQL Federation. It is about moving toward a common standard that the entire ecosystem can build on.

**The future of federation is open.**

# Get started with GraphQL Federation

You don't need to rebuild your existing APIs to get started.

Fusion is a high-performance GraphQL gateway that supports both Apollo Federation and GraphQL Federation, allowing you to connect existing subgraphs and adopt the open specification at your own pace.

Whether you're starting a new federated graph or already running Apollo Federation, Fusion lets you bring your services together without locking your architecture into a single federation ecosystem.

Performance matters, too. Our [open GraphQL Federation benchmarks](/benchmarks/graphql-federation) compare gateways under identical conditions, measuring throughput, latency, memory consumption, and CPU usage. Explore the results and see how your gateway stacks up.

[Explore Fusion](/products/fusion) | [View Benchmarks](/benchmarks/graphql-federation)
