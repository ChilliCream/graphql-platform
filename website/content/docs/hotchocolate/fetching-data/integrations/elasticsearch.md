---
title: Elasticsearch
description: Learn how to integrate Elasticsearch with Hot Chocolate through ES|QL, including filtering, sorting, and pagination.
---

> [!EXPERIMENTAL]
> The Elasticsearch integration is currently in preview and its API may change in future releases.

Hot Chocolate has a data integration for Elasticsearch. It runs filtering, sorting, and the first page of a query as [ES|QL](https://www.elastic.co/docs/reference/query-languages/esql), using the official `Elastic.Clients.Esql` client.

# Get Started

Install the `HotChocolate.Data.ElasticSearch` package:

<PackageInstallation packageName="HotChocolate.Data.ElasticSearch" />

Its APIs are marked as experimental with the `HC8003` diagnostic. Suppress it in the project file to use them:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);HC8003</NoWarn>
</PropertyGroup>
```

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

A field whose resolver does not return an ES|QL query, such as a list in memory or an Entity Framework query, is filtered as with `AddFiltering`.

Negative conditions such as `neq` and `nin` keep the documents where the field has no value, as in .NET. For a field of an object, this needs another field of the object to have a value: Elasticsearch stores nothing for an object whose fields are all null, so the document is treated as if the object were missing.

## Lists of objects

An object mapping stores the values of each field in a column of its own, so it cannot tell which values belong to the same object. Map the list as `nested` and mark the property with `ElasticSearchNested` to combine several conditions on one object, or to use `all` and negative conditions:

```csharp
public class Product
{
    public string Name { get; set; } = "";

    [ElasticSearchNested]
    public List<Review> Reviews { get; set; } = [];
}
```

Without the attribute, a list of objects answers `some`, `none` and `any` with a single condition on a single field, and any other filter returns an error that names the field. It does not see an element whose fields are all null, so `any` counts only the elements with a value.

In a list of objects, an enum is compared by name when the property or the enum has a `JsonStringEnumConverter` attribute, and by number otherwise.

Filters on a `nested` field are tested on Elasticsearch 9.5. On 9.1, the KQL function behind them ignores the case of the value. A `startsWith` on a `nested` field cannot contain a character that is reserved in KQL.

# Sorting

Register the sorting convention on the schema builder:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddElasticSearchSorting();
```

# Pagination

ES|QL cannot skip rows, so a query can read the first page only. A request that would skip rows, such as one with `skip` or `after`, returns an error. Register the offset paging provider to read the first page and the total count asynchronously:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddElasticSearchPagingProviders();
```

# Limitations

Reading a list of objects in the result is not supported yet: the list can be filtered, but comes back empty when selected.
