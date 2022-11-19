---
title: Marten
---

Generally, the features from the `HotChocolate.Data` package should work with
any LINQ provider from which some `IQueryable<T>` can be retrieved. However, this is not the case with Marten. Pagination and projections
work out of the box as expected, but filtering and sorting do not. LINQ expressions generated for filtering and sorting must first
be translated in a format that is digestible for the Marten LINQ provider before they are applied to the `IQueryable<T>` object.
This integration provides custom configurations to seamlessly integrate Marten with the `HotChocolate.Data` package.

# Get Started

To use the MongoDB integration, you need to install the package `HotChocolate.Data.Marten`.

```bash
dotnet add package HotChocolate.Data.Marten
```

> ⚠️ Note: All `HotChocolate.*` packages need to have the same version.

# Filtering

To use Marten filtering, you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMartenFiltering();
```
[Learn more about filtering](/docs/hotchocolate/v13/fetching-data/filtering).

# Sorting

To use Marten sorting, you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMartenSorting();
```
[Learn more about sorting](/docs/hotchocolate/v13/fetching-data/sorting).

# Projections

Projections work out of the box as expected with Marten. No custom configuration is needed.
[Learn more about projections](/docs/hotchocolate/v13/fetching-data/projections).

# Paging

Pagination works out of the box as expected with Marten. No custom configuration is needed.
[Learn more about pagination](/docs/hotchocolate/v13/fetching-data/pagination).
