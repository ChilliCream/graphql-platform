---
title: "Overview"
---

Fusion lets you split a GraphQL API across multiple independent services (subgraphs) while exposing a single, unified schema to clients. Clients query one endpoint; the gateway figures out which subgraphs to call and assembles the response.

# What Is Fusion

Fusion is a distributed GraphQL architecture built on HotChocolate. You build each subgraph as a standard HotChocolate server, add a few annotations to describe how your types relate to the bigger graph, and let the Fusion gateway handle cross-service coordination.

The architecture has three parts:

```text
                    ┌──────────┐
                    │  Client  │
                    └────┬─────┘
                         │
                         ▼
                ┌────────────────┐
                │ Fusion Gateway │
                └──┬──────┬──┬───┘
                   │      │  │
          ┌────────┘      │  └────────┐
          ▼               ▼           ▼
   ┌────────────┐  ┌────────────┐  ┌────────────┐
   │  Products  │  │  Accounts  │  │  Reviews   │
   │  Subgraph  │  │  Subgraph  │  │  Subgraph  │
   └──────┬─────┘  └──────┬─────┘  └──────┬─────┘
          │               │               │
          ▼               ▼               ▼
   ┌────────────┐  ┌────────────┐  ┌────────────┐
   │ Database A │  │ Database B │  │ Database C │
   └────────────┘  └────────────┘  └────────────┘
```

**Subgraphs** are HotChocolate servers with a few extra annotations. They own their data and their portion of the schema. They never call each other, never import each other's code, and can be deployed independently.

**Composition** merges all subgraph schemas into a single composite schema and produces a gateway configuration file. This happens offline -- through the Nitro CLI or .NET Aspire -- not at runtime. If two subgraphs define conflicting types, composition fails with an error _before_ you deploy.

**The gateway** receives client queries, plans which subgraphs to call based on the composed configuration, executes those calls, and merges the results. You do not write routing logic or resolver code in the gateway.

The result: a client sends a single query, the gateway fans it out to the relevant subgraphs, and the response comes back as if it were a monolithic API.

<!-- prettier-ignore-start -->
```graphql
# This query touches three subgraphs, but the client doesn't know or care.
query {
  products(first: 5) {
    nodes {
      name          # from Products subgraph
      price         # from Products subgraph
      reviews {     # from Reviews subgraph
        stars
        author {
          username  # from Accounts subgraph
        }
      }
    }
  }
}
```
<!-- prettier-ignore-end -->

## Three Things That Make Fusion Different

**Lookups are real Query fields.** When the gateway needs to resolve an entity from a subgraph, it calls a normal Query field annotated with `[Lookup]`. You can call the same field yourself in testing, debug it with standard tools, and see exactly what it returns. There is no hidden `_entities` field or magic resolution protocol.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static async Task<Product?> GetProductById(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => await productById.LoadAsync(id, cancellationToken);
}
```

**Composition catches errors at build time.** When you run `nitro fusion compose`, the composition engine validates all subgraph schemas against each other. Type conflicts, missing fields, incompatible enums -- these are caught now, in your CI pipeline, not at 3 AM when a user hits a broken query path.

**No federation runtime in your subgraphs.** Your subgraphs are standard HotChocolate servers. There is no subgraph library to install, no federation middleware to configure, no vendor-specific protocol. Your schemas follow the open [GraphQL Composite Schemas specification](https://graphql.github.io/composite-schemas-spec/draft/) being developed under the GraphQL Foundation.

# Key Terminology

| Term                 | Definition                                                                                                                                                                     |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Subgraph**         | A HotChocolate server that owns a portion of the overall schema. Each subgraph manages its own data and resolvers.                                                             |
| **Source schema**    | The GraphQL schema exported by a single subgraph. This is what gets fed into composition.                                                                                      |
| **Composite schema** | The unified, client-facing schema produced by merging all source schemas. Clients query this schema as if it were a single API.                                                |
| **Gateway**          | The entry point for client requests. It receives queries against the composite schema, plans execution across subgraphs, and assembles responses.                              |
| **Entity**           | A type that appears in more than one subgraph. For example, both the Products and Reviews subgraphs may define a `Product` type, each contributing different fields.           |
| **Lookup**           | A Query field annotated with `[Lookup]` that the gateway uses to resolve an entity from a subgraph. It is a standard, callable query field -- not a hidden internal mechanism. |
| **Composition**      | The offline process of validating and merging source schemas into a composite schema and gateway configuration. Runs via the Nitro CLI or Aspire, not at runtime.              |

# When to Use Fusion

Fusion adds operational complexity -- a gateway process, a composition step in your build pipeline, distributed debugging. That complexity pays off in specific situations:

- **Multiple teams need to ship independently.** If different teams own different parts of your API (e.g., a product catalog team and a reviews team), Fusion lets each team deploy on their own schedule without coordinating schema changes through a shared codebase.

- **You need to scale services differently.** Your product search might need 10 instances while your user profile service needs 2. With separate subgraphs, you scale each service based on its actual load.

- **Your domain has clear boundaries.** If your data naturally splits into distinct areas (accounts, products, orders, reviews), subgraphs map well to those boundaries. Each subgraph owns its data store and its portion of the schema.

- **You want build-time validation of your distributed schema.** Composition catches conflicts between subgraphs before deployment. Your CI pipeline can validate that a schema change in one subgraph does not break the composed graph.

# When NOT to Use Fusion

Fusion is not the right choice for every project. Be honest about whether you need it:

- **One team, one service.** If a single team owns the entire API and deploys it as one unit, a standard HotChocolate server is simpler and has less operational overhead. You do not need a gateway, a composition pipeline, or distributed tracing for a single service.

- **A small or early-stage API.** If your API has a handful of types and a few hundred queries per second, the added complexity of federation is not justified. Start with a monolith. You can split it later.

- **No clear domain boundaries.** If your types are deeply intertwined and nearly every query touches every part of the schema, splitting into subgraphs will create more cross-service calls than it eliminates. Federation works best when subgraphs are relatively self-contained.

- **Your team is just getting started with GraphQL.** Learn HotChocolate first. Get comfortable with types, resolvers, DataLoaders, and the execution pipeline. Fusion adds concepts on top of that foundation -- it is easier to adopt once the basics are solid.

The cost of premature federation is real: more services to deploy, more infrastructure to monitor, harder debugging when something goes wrong. Start simple, and add Fusion when the pain of a monolith outweighs the cost of distribution.

# Migrating from a Monolith

If you already have a HotChocolate server, you are closer to Fusion than you think. Your existing server is already a valid subgraph -- it just happens to be the only one.

**Start with a "graph of one."** Point the Fusion gateway at your existing HotChocolate server as a single subgraph. Composition works with one source schema. Your clients connect to the gateway instead of directly to your server, but the behavior is identical. Nothing breaks.

**Add subgraphs incrementally.** When a new team or a new domain needs its own service, create a second subgraph. The new subgraph can extend types from the original server using entity stubs. Composition merges both schemas. The gateway handles cross-subgraph queries automatically. Your original server does not change.

**Clients see no difference.** Whether you have one subgraph or ten, the composite schema looks the same to clients. You can split your monolith over weeks or months without ever breaking the client contract.

The key insight: federation is not a rewrite. It is a gradual process. You move types and fields to new subgraphs one at a time, and the gateway smooths over the transition.

# Next Steps

Where you go from here depends on what you need:

- **"I want to build something."** Start with the [Getting Started](/docs/fusion/v16/getting-started) tutorial. You will create two subgraphs and a gateway from scratch.

- **"I want to add a subgraph to an existing project."** Go to [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph). It covers creating a new subgraph that extends existing entity types.

- **"I'm migrating from another federation framework."** Read [Coming from Apollo Federation](/docs/fusion/v16/coming-from-apollo-federation) or [Migrating from Schema Stitching](/docs/fusion/v16/migrating-from-schema-stitching). These guides map familiar concepts to Fusion equivalents and walk through a migration.

- **"I need to deploy this."** See [Deployment & CI/CD](/docs/fusion/v16/deployment-and-ci-cd) for pipeline setup, schema management, and gateway configuration.
