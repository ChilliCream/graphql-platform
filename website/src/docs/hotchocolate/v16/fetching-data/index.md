---
title: Overview
---

Hot Chocolate provides data middleware that applies common operations directly to your `IQueryable` or `IExecutable` data sources. Instead of implementing pagination, filtering, sorting, and projections by hand, you declare them on your fields and Hot Chocolate generates the corresponding GraphQL types and applies the operations at execution time.

# Pagination

Hot Chocolate provides cursor-based connection pagination out of the box. Connections follow the [Relay Cursor Connections Specification](https://relay.dev/graphql/connections.htm), giving clients a standardized way to page through large datasets. When backed by `IQueryable`, pagination translates directly to native database queries.

[Learn more about pagination](/docs/hotchocolate/v16/fetching-data/pagination)

# Filtering

When you return a list of entities, clients often need to filter them by operations like `equals`, `contains`, or `startsWith`. Hot Chocolate generates the necessary filter input types from your .NET models and translates applied filters into native database queries.

[Learn more about filtering](/docs/hotchocolate/v16/fetching-data/filtering)

# Sorting

Hot Chocolate generates sort input types from your .NET models, allowing clients to specify which fields to sort by and in which direction. Like filtering, sort operations translate to native database queries when backed by `IQueryable`.

[Learn more about sorting](/docs/hotchocolate/v16/fetching-data/sorting)

# Projections

Projections optimize database queries by selecting only the columns that match the fields requested in the GraphQL query. If a client requests `name` and `id`, Hot Chocolate queries only those columns from the database.

[Learn more about projections](/docs/hotchocolate/v16/fetching-data/projections)

# Batching

DataLoaders and batch resolvers solve the N+1 problem in GraphQL. When the execution engine resolves a list of objects and each needs related data, a DataLoader collects all individual requests and sends a single query for all keys at once.

- [DataLoader](/docs/hotchocolate/v16/fetching-data/batching/dataloader) for key-based batching with deduplication and caching.
- [Batch Resolvers](/docs/hotchocolate/v16/fetching-data/batching/batch-resolver) for simpler cases where caching is not needed.

# Integrations

Hot Chocolate is not bound to a specific database. The data middleware works with any `IQueryable` provider. We provide specific guidance for the most common data sources:

- [Entity Framework](/docs/hotchocolate/v16/fetching-data/integrations/entity-framework) for EF Core DbContext patterns and pooling.
- [MongoDB](/docs/hotchocolate/v16/fetching-data/integrations/mongodb) for the MongoDB driver integration.
- [Marten](/docs/hotchocolate/v16/fetching-data/integrations/marten) for Marten document database support.
- [Extending Filtering](/docs/hotchocolate/v16/fetching-data/integrations/extending-filtering) for building custom filter providers.
