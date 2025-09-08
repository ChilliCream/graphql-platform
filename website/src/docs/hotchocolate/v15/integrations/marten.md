---
title: Marten
---

Generally, the features from the `HotChocolate.Data` package should work with
any LINQ provider from which some `IQueryable<T>` can be retrieved. However, this is not the case with Marten. Pagination and projections
work out of the box as expected, but filtering and sorting do not. LINQ expressions generated for filtering and sorting must first
be translated in a format that is digestible for the Marten LINQ provider before they are applied to the `IQueryable<T>` object.
This integration provides custom configurations to seamlessly integrate Marten with the `HotChocolate.Data` package.

You can find a sample project for the integration in [Hot Chocolate Examples](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/MartenDB).

# Get Started

To use the Marten integration, you need to install the package `HotChocolate.Data.Marten`.

<PackageInstallation packageName="HotChocolate.Data.Marten" />

# Filtering

To use Marten filtering, you need to register the convention on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMartenFiltering();
```

[Learn more about filtering](/docs/hotchocolate/v15/fetching-data/filtering).

# Sorting

To use Marten sorting, you need to register the convention on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMartenSorting();
```

[Learn more about sorting](/docs/hotchocolate/v15/fetching-data/sorting).

# Projections

Projections work out of the box as expected with Marten. No custom configuration is needed.
[Learn more about projections](/docs/hotchocolate/v15/fetching-data/projections).

# Paging

Pagination works out of the box as expected with Marten. No custom configuration is needed.
[Learn more about pagination](/docs/hotchocolate/v15/fetching-data/pagination).
