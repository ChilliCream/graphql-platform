---
title: Marten
description: Learn how to integrate Marten with Hot Chocolate v16 for filtering, sorting, projections, and pagination.
---

The `HotChocolate.Data` package generally works with any LINQ provider that provides an `IQueryable<T>`. However, Marten requires special handling. Pagination and projections work out of the box, but filtering and sorting need LINQ expressions translated into a format that the Marten LINQ provider can process. This integration provides custom configurations for that purpose.

You can find a sample project in [Hot Chocolate Examples](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/MartenDB).

# Get Started

Install the `HotChocolate.Data.Marten` package:

<PackageInstallation packageName="HotChocolate.Data.Marten" />

# Filtering

Register the Marten filtering convention on the schema builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMartenFiltering();
```

[Learn more about filtering](../resolvers-and-data/filtering.md).

# Sorting

Register the Marten sorting convention on the schema builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMartenSorting();
```

[Learn more about sorting](../resolvers-and-data/sorting.md).

# Projections

Projections work out of the box with Marten. No custom configuration is needed.

[Learn more about projections](../resolvers-and-data/projections.md).

# Paging

Pagination works out of the box with Marten. No custom configuration is needed.

[Learn more about pagination](../resolvers-and-data/pagination.md).

# Next Steps

- [Filtering](../resolvers-and-data/filtering.md) for filtering concepts
- [Sorting](../resolvers-and-data/sorting.md) for sorting concepts
- [Pagination](../resolvers-and-data/pagination.md) for pagination setup
