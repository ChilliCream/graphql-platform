---
title: "Batching"
metaTitle: "GraphQL Batching in Hot Chocolate: Solve the N+1 Problem"
description: "Learn how batching solves the N+1 problem in GraphQL. Hot Chocolate offers DataLoader and batch resolvers to group data fetches into single calls."
---

Batching groups many small data fetches into one call to your data source and is the standard answer to the N+1 problem in GraphQL. Because GraphQL resolves data field by field, you should plan for batching from the start: it determines whether your API issues one query per request or hundreds.

This chapter explains why the N+1 problem appears in GraphQL and introduces the two batching tools Hot Chocolate provides: [DataLoader](./dataloader.md) and [batch resolvers](./batch-resolver.md).

> [!NOTE]
> This chapter is about batching data fetches inside a single GraphQL operation. If you are looking for a way to send multiple GraphQL operations in one HTTP request, see [Server Batching](../../server/batching.md).

# Why the N+1 Problem Happens in Every API

The N+1 problem exists with any API technology. A REST client that fetches a list of products and then calls `/brands/{id}` for each product produces the exact same access pattern: one request for the list, plus N requests for related data. The same happens inside a REST backend that loads a list and then lazily loads a relation per item.

GraphQL does not make this problem worse. What GraphQL changes is where the problem lives. With REST, each client decides how to stitch resources together, so the N+1 pattern is distributed across every client and cannot be fixed centrally. With GraphQL, clients declare what they need in one query, and the server resolves it. The deliberate decision is to handle data fetching centrally in the backend, and the backend is exactly where it can be optimized well: it is one place, it is close to the data, and one fix helps every client.

# Why Per-Field Resolvers Make N+1 Visible

Every field in a GraphQL schema is backed by a [resolver](../../resolvers/index.md), and the execution engine walks the query as a tree. Consider this query:

**Client query**

```graphql
{
  products(first: 5) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

The `products` resolver runs once and returns five products. Then the `brand` resolver runs once per product. A naive `brand` resolver that queries the database directly issues five queries for five products, and fifty queries for fifty products:

```text
1 query:  products(first: 5)
5 queries: brand for product 1..5   ← N+1
```

The per-field resolver model is what makes the N+1 pattern visible and measurable in GraphQL. That visibility is a feature: because all data fetching flows through resolvers, you can intercept it in one place and batch it.

# DataLoader

A DataLoader batches by key. Resolvers ask the DataLoader for a value by key, the DataLoader collects all requested keys while the engine executes resolvers, and then fetches all of them in one call (for example a single `WHERE id IN (...)` query). DataLoaders also cache and deduplicate within a request: the same key requested from anywhere in the query tree is fetched once, and every resolver sees the same result.

DataLoaders are the default choice for batching in Hot Chocolate. Use them whenever data is loaded by key and may be requested from more than one place in a query.

[Learn more about DataLoader](./dataloader.md)

# Batch Resolvers

A batch resolver batches by field. Instead of running a resolver once per parent object, the execution engine collects all parent objects that reach a specific field and calls your resolver once with the full list. You do not define a DataLoader class or a key: you receive the parents and return one result per parent.

Batch resolvers have no cache and no cross-field deduplication. They fit computed values, aggregations, and external services with native batch endpoints, where the result is specific to one field.

[Learn more about Batch Resolvers](./batch-resolver.md)

# Next Steps

- **Loading data by key?** Start with [DataLoader](./dataloader.md).
- **Resolving one field for many parents?** See [Batch Resolvers](./batch-resolver.md).
- **New to resolvers?** See [Resolvers](../../resolvers/index.md) for the resolver tree mental model.
- **Using Entity Framework?** See [Entity Framework](../integrations/entity-framework.md) for integration patterns.
- **Looking for HTTP request batching?** See [Server Batching](../../server/batching.md).
