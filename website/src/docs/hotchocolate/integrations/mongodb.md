---
title: MongoDB
---

HotChocolate has a data integration for MongoDB.
With this integration, you can translate paging, filtering, sorting, and projections, directly into native MongoDB queries.

You can find a example project in [HotChocolate Examples](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/MongoDB)

# Get Started

To use the MongoDB integration, you need to install the package `HotChocolate.Data.MongoDb`.

```bash
dotnet add package HotChocolate.Data.MongoDb
```

# MongoExecutable

The whole integration builds around `IExecutable<T>`.
The integration provides you the extension method `AsExecutable` on `IMongoCollection<T>`, `IAggregateFluent<T>` and `IFindFluent<T>`
The execution engine picks up the `IExecutable` and executes it efficiently.
You are free to use any form of aggregation or find a pipeline before you execute `AsExecutable`

```csharp
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons([Service] IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}

[UseFirstOrDefault]
public IExecutable<Person> GetPersonById(
    [Service] IMongoCollection<Person> collection,
    Guid id)
{
    return collection.Find(x => x.Id == id).AsExecutable();
}
```

# Filtering

To use MongoDB filtering you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbFiltering();
```

> To use MongoDB filtering alongside with `IQueryable`/`IEnumerable`, you have to register the MongoDB convention under a different scope.
> You can specify the scope on the schema builder by executing `AddMongoDbFiltering("yourScope")`.
> You then have to specify this scope on each method you use MongoDb filtering: `[UseFiltering(Scope = "yourScope")]` or `UseFiltering(scope = "yourScope")`

Your filters are now converted to `BsonDocument`s and applied to the executable.

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

_Mongo Query_

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

To use MongoDB sorting you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbSorting();
```

> To use MongoDB Sorting alongside with `IQueryable`/`IEnumerable`, you have to register the MongoDB convention under a different scope.
> You can specify the scope on the schema builder by executing `AddMongoDbSorting("yourScope")`.
> You then have to specify this scope on each method you use MongoDb Sorting: `[UseSorting(Scope = "yourScope")]` or `UseSorting(scope = "yourScope")`

Your sorting is now converted to `BsonDocument`s and applied to the executable.

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

_Mongo Query_

```json
{
  "find": "person",
  "filter": {},
  "sort": { "Name": 1, "MainAddress.City": -1 }
}
```

# Projections

To use MongoDB projections you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMongoDbProjections();
```

> To use MongoDB Projections alongside with `IQueryable`/`IEnumerable`, you have to register the MongoDB convention under a different scope.
> You can specify the scope on the schema builder by executing `AddMongoDbProjections("yourScope")`.
> You then have to specify this scope on each method you use MongoDb Projections: `[UseProjections(Scope = "yourScope")]` or `UseProjections(scope = "yourScope")`

Projections do not always lead to a performance increase.
Even though MongoDB processes and transfers less data, it more often than not harms query performance.
This [Medium article by Tek Loon](https://betterprogramming.pub/improve-mongodb-performance-using-projection-c08c38334269) explains how and when to use projections well.

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

_Mongo Query_

```json
{
  "find": "person",
  "filter": {},
  "projection": { "Addresses.City": 1, "Name": 1 }
}
```

# Paging

The integration comes with providers for offset and cursor-based pagination

## Cursor Pagination

To use cursor based pagination annoate you resolver with `[UseMongoDbPaging]` or `.UseMongoDbPaging()`

```csharp
[UseMongoDbPaging]
public IExecutable<Person> GetPersons([Service] IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}
```

You can then execute queries like the following one:

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

## Offset Pagination

To use cursor based pagination annoate you resolver with `[UseMongoDbPaging]` or `.UseMongoDbPaging()`

```csharp
[UseMongoDbOffsetPaging]
public IExecutable<Person> GetPersons([Service] IMongoCollection<Person> collection)
{
    return collection.AsExecutable();
}
```

You can then execute queries like the following one:

```graphql
query GetPersons {
  persons(skip: 50, take: 50) {
    items {
      name
      addresses {
        city
      }
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
  }
}
```

# FirstOrDefault / SingleOrDefault

Sometimes you may want to return only a single object of a collection.
To limit the response to one element you can use the `UseFirstOrDefault` or `UseSingleOrDefault` middleware.
HotChocolate will rewrite the type of the field from a list type to an object type.

```csharp
[UseFirstOrDefault]
public IExecutable<Person> GetPersonById(
    [Service] IMongoCollection<Person> collection,
    Guid id)
{
    return collection.Find(x => x.Id == id).AsExecutable();
}
```
