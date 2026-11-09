---
title: Elasticsearch
description: Learn how to integrate Elasticsearch with Hot Chocolate through ES|QL, including filtering, sorting, and pagination.
---

Hot Chocolate has a data integration for Elasticsearch. It runs filtering, sorting, and the first page of a query as [ES|QL](https://www.elastic.co/docs/reference/query-languages/esql), using the official `Elastic.Clients.Esql` client.

# Get Started

Install the `HotChocolate.Data.ElasticSearch` package:

<PackageInstallation packageName="HotChocolate.Data.ElasticSearch" />

Return the query as an executable, so that it runs asynchronously:

```csharp
[UseOffsetPaging]
[UseSorting]
[UseFiltering]
public IExecutable<Product> GetProducts(EsqlClient client)
{
    return client.CreateQuery<Product>().From("products").AsElasticSearchExecutable();
}
```

# Filtering

Register the filtering convention on the schema builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddElasticSearchFiltering();
```

Field names are camel case by default. If the client uses another naming policy, pass it to `AddElasticSearchFiltering(namingPolicy)`. A `JsonPropertyName` attribute on a property takes precedence.

## Lists of objects

An object mapping stores the values of each field in a column of its own, so it cannot tell which values belong to the same object. Map the list as `nested` and mark the property with `ElasticSearchNested` to filter on several fields of one object, or to use `all` and negative conditions:

```csharp
public class Product
{
    public string Name { get; set; } = "";

    [ElasticSearchNested]
    public List<Review> Reviews { get; set; } = [];
}
```

Without the attribute, a list of objects answers `some`, `none` and `any` on a single field, and any other filter returns an error that names the field.

Filters on a `nested` field are tested on Elasticsearch 9.5. On 9.1, the KQL function behind them ignores the case of the value. A `startsWith` on a `nested` field cannot contain a character that is reserved in KQL.

# Sorting

The default sorting convention works without further configuration:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddSorting();
```

# Pagination

ES|QL cannot skip rows, so a query can read the first page only. Requests with `skip`, `after`, or `before` return an error. Register the offset paging provider to read the first page and the total count asynchronously:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddElasticSearchPagingProviders();
```

# Limitations

Reading a list of objects in the result is not supported yet: the list can be filtered, but comes back empty when selected.
