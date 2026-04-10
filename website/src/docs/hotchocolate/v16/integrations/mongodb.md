---
title: MongoDB
description: Learn how to integrate MongoDB with Hot Chocolate v16, including filtering, sorting, projections, and pagination.
---

Hot Chocolate has a data integration for MongoDB. With this integration, you can translate paging, filtering, sorting, and projections directly into native MongoDB queries.

You can find an example project in [Hot Chocolate Examples](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/MongoDB).

# Get Started

Install the `HotChocolate.Data.MongoDb` package:

<PackageInstallation packageName="HotChocolate.Data.MongoDb" />

# MongoExecutable

The integration builds around `IExecutable<T>`. The `AsExecutable` extension method is available on `IMongoCollection<T>`, `IAggregateFluent<T>`, and `IFindFluent<T>`. The execution engine picks up the `IExecutable` and executes it efficiently. You can use any aggregation or find pipeline before calling `AsExecutable`.

```csharp
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons(IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}

[UseFirstOrDefault]
public IExecutable<Person> GetPersonById(
    IMongoCollection<Person> collection, Guid id)
{
    return collection.Find(x => x.Id == id).AsExecutable();
}
```

# Filtering

Register the MongoDB filtering convention on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbFiltering();
```

> To use MongoDB filtering alongside `IQueryable`/`IEnumerable`, register the MongoDB convention under a different scope: `AddMongoDbFiltering("yourScope")`. Then specify the scope on each resolver: `[UseFiltering(Scope = "yourScope")]`.

Filters are converted to `BsonDocument`s and applied to the executable.

_GraphQL Query:_

```graphql
query GetPersons {
  persons(
    where: {
      name: { eq: "Yorker Shorton" }
      addresses: { some: { street: { eq: "04 Leroy Trail" } } }
    }
  ) {
    name
    addresses {
      street
      city
    }
  }
}
```

_Mongo Query:_

```json
{
  "find": "person",
  "filter": {
    "Name": { "$eq": "Yorker Shorton" },
    "Addresses": { "$elemMatch": { "Street": { "$eq": "04 Leroy Trail" } } }
  }
}
```

# Sorting

Register the MongoDB sorting convention on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbSorting();
```

> To use MongoDB sorting alongside `IQueryable`/`IEnumerable`, register the MongoDB convention under a different scope: `AddMongoDbSorting("yourScope")`. Then specify the scope on each resolver: `[UseSorting(Scope = "yourScope")]`.

Sorting is converted to `BsonDocument`s and applied to the executable.

_GraphQL Query:_

```graphql
query GetPersons {
  persons(order: [{ name: ASC }, { mainAddress: { city: DESC } }]) {
    name
    addresses {
      street
      city
    }
  }
}
```

_Mongo Query:_

```json
{
  "find": "person",
  "filter": {},
  "sort": { "Name": 1, "MainAddress.City": -1 }
}
```

# Projections

Register the MongoDB projection convention on the schema builder:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbProjections();
```

> To use MongoDB projections alongside `IQueryable`/`IEnumerable`, register the MongoDB convention under a different scope: `AddMongoDbProjections("yourScope")`. Then specify the scope on each resolver: `[UseProjection(Scope = "yourScope")]`.

Projections do not always improve performance. Even though MongoDB processes and transfers less data, projections can sometimes harm query performance. See [this article by Tek Loon](https://betterprogramming.pub/improve-mongodb-performance-using-projection-c08c38334269) for guidance on when to use them.

_GraphQL Query:_

```graphql
query GetPersons {
  persons {
    name
    addresses {
      city
    }
  }
}
```

_Mongo Query:_

```json
{
  "find": "person",
  "filter": {},
  "projection": { "Addresses.City": 1, "Name": 1 }
}
```

# Paging

Register the MongoDB-specific pagination providers:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders();
```

[Learn more about pagination providers](/docs/hotchocolate/v16/resolvers-and-data/pagination#providers)

## Cursor Pagination

Annotate your resolver with `[UsePaging]` or `.UsePaging()` to use cursor-based pagination:

```csharp
[UsePaging]
public IExecutable<Person> GetPersons(IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}
```

Example query:

```graphql
query GetPersons {
  persons(first: 50, after: "OTk=") {
    nodes {
      name
      addresses {
        city
      }
    }
    pageInfo {
      endCursor
      hasNextPage
      hasPreviousPage
      startCursor
    }
  }
}
```

# FirstOrDefault / SingleOrDefault

To return a single object from a collection, use the `UseFirstOrDefault` or `UseSingleOrDefault` middleware. Hot Chocolate rewrites the field type from a list to an object type.

```csharp
[UseFirstOrDefault]
public IExecutable<Person> GetPersonById(
    IMongoCollection<Person> collection, Guid id)
{
    return collection.Find(x => x.Id == id).AsExecutable();
}
```

# Next Steps

- [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) for pagination setup
- [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) for filtering concepts
- [Executable](/docs/hotchocolate/v16/api-reference/executable) for the `IExecutable` abstraction

<!-- spell-checker:ignore Shorton -->
