---
title: "GraphQL Federation vs. Backend for Frontend (BFF)"
metaTitle: "GraphQL Federation vs BFF: Differences & When to Use Each"
description: "Compare GraphQL Federation and the Backend for Frontend (BFF) pattern. Learn how they differ, when to use each, and how they work together."
date: "2026-09-08"
tags: ["graphql-federation", "comparison", "bff"]
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

GraphQL Federation and the Backend for Frontend (BFF) pattern both help applications work with data distributed across multiple services. However, they approach API architecture from different directions.

A BFF provides an API tailored to the needs of a particular client, such as a web application or mobile app. GraphQL Federation brings independently developed services together into one unified graph that multiple clients can query.

**The fundamental difference is where you establish your architectural boundaries: around clients or around business domains.**

Understanding this distinction helps determine when to use either approach and why they often work well together.

# What is a Backend for Frontend?

The Backend for Frontend pattern introduces a dedicated backend for a particular client or user experience.

For example, a web application and a mobile application might have different requirements. The web application may need detailed product information, while the mobile application prioritizes smaller responses and fewer network requests.

With the BFF pattern, each client can have a backend responsible for retrieving data, orchestrating services, and exposing an API optimized for its needs.

This gives frontend teams control over how their applications interact with backend systems. They can evolve their APIs alongside their clients without coordinating every change through a shared API team.

BFFs are particularly useful when clients require specialized workflows, authentication handling, or orchestration that doesn't belong in the underlying services.

# Where BFF architectures become challenging

As applications grow, multiple BFFs may begin consuming the same backend services.

A web BFF, mobile BFF, and partner BFF might all need to retrieve product information, customer details, and order histories.

If each BFF independently implements the relationships between these services, the same aggregation logic can end up being maintained in several places.

This introduces additional work:

- Changes to underlying services may affect multiple BFFs.
- Shared relationships and data-fetching logic can become duplicated.
- Different clients may implement the same behavior inconsistently.
- Every additional BFF introduces another service to maintain and operate.

These problems aren't inherent to the BFF pattern. Shared libraries, dedicated aggregation services, and other architectural approaches can address them.

However, when many clients need access to the same connected data, it may make sense to establish that shared data model independently of individual applications.

# How GraphQL Federation approaches the problem

GraphQL Federation organizes an API around independently owned services rather than individual clients.

Each service contributes part of a unified GraphQL schema. These services, called subgraphs, can represent different business domains and evolve independently.

A process called schema composition combines their definitions into one composite schema, while a GraphQL gateway executes queries across the services behind it.

Instead of implementing relationships between services separately in every BFF, those relationships become part of the shared graph.

For example, a product catalog can expose products while an inventory service contributes availability information to the same product type.

Clients can query both through one GraphQL API:

```graphql
query {
  productById(id: "123") {
    name
    price
    availability {
      inStock
    }
  }
}
```

The gateway coordinates the necessary requests across the catalog and inventory subgraphs and returns one response.

A web application might request additional product details, while a mobile application asks for fewer fields. Both use the same graph without requiring separate aggregation implementations.

This is one of GraphQL Federation's main advantages: **the relationships between business domains are defined once and can be reused by every client.**

# GraphQL Federation and BFFs can work together

Choosing GraphQL Federation doesn't mean abandoning the Backend for Frontend pattern.

A BFF can sit between a client and a federated GraphQL API, providing client-specific functionality while relying on the shared graph for data access.

For example, a mobile BFF might manage sessions, coordinate a client-specific workflow, or integrate with an external service. Instead of independently aggregating data from several backend services, it queries the federated graph.

This creates a clear separation of responsibilities:

- **GraphQL Federation** provides a unified API organized around business domains.
- **Backend for Frontend** handles the requirements specific to an individual client.

The result is an architecture where domain relationships are shared, while frontend teams retain the flexibility to build experiences tailored to their applications.

In some cases, GraphQL Federation can eliminate the need for a BFF entirely. In others, the two approaches complement each other.

# GraphQL Federation vs. BFF at a glance

|                    | GraphQL Federation                               | Backend for Frontend                    |
| ------------------ | ------------------------------------------------ | --------------------------------------- |
| Primary focus      | Business domains and shared data                 | Individual clients                      |
| API structure      | Unified GraphQL schema                           | Client-specific API                     |
| Ownership          | Domain and service teams                         | Typically frontend teams                |
| Data aggregation   | Shared through the graph                         | Implemented or orchestrated per BFF     |
| Client flexibility | Clients select the fields they need              | Backend tailored to client requirements |
| Best suited for    | Shared data across multiple services and clients | Client-specific behavior and workflows  |

# When should you use GraphQL Federation or a BFF?

**Choose GraphQL Federation** when multiple applications need access to the same connected data, and you want service teams to own their domains independently. Federation provides a shared API without requiring every client team to implement relationships between backend services.

**Choose a Backend for Frontend** when applications have distinct requirements that benefit from dedicated backend logic, such as specialized workflows, session management, or integrations.

**Use both** when you want a shared domain model across your organization while preserving client-specific backend functionality.

The decision isn't necessarily between GraphQL Federation and BFFs. It's about identifying which responsibilities belong to your business domains and which belong to individual clients.

# Build a unified API with Fusion

Fusion is a high-performance GraphQL gateway that brings GraphQL, REST, and gRPC services together into one unified graph.

It supports both the GraphQL Federation specification and Apollo Federation, allowing teams to expose their existing services through a shared API without requiring a coordinated migration.

Whether your clients access the graph directly or through dedicated BFFs, Fusion provides a common foundation for querying data across your organization.

[Explore Fusion](/products/fusion) | [Learn about GraphQL Federation](/platform/graphql-federation)
