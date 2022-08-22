---
title: Neo4j Database
---

HotChocolate has a data integration for Neo4j.
With this integration, you can translate paging, filtering, sorting, and projections, directly into native Cypher queries.

You can find a example project in [HotChocolate Examples](https://github.com/ChilliCream/graphql-workshop-Neo4j)

# Get Started

To use the Neo4j integration, you need to install the package `HotChocolate.Data.Neo4J`.

```bash
dotnet add package HotChocolate.Data.Neo4J
```

> ⚠️ Note: All `HotChocolate.*` packages need to have the same version.

# Neo4JExecutable

The whole integration builds around `IExecutable<T>`.
The execution engine picks up the `IExecutable` and executes it efficiently.

```csharp
[UseNeo4JDatabase("neo4j")]
[UseOffsetPaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) => new Neo4jExecutable<Person>(session);
```

# Filtering

To use Neo4j filtering you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JFiltering();
```

> To use Neo4j filtering alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4j convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JFiltering("yourScope")`.
> You then have to specify this scope on each method you use Neo4j filtering: `[UseFiltering(Scope = "yourScope")]` or `UseFiltering(scope = "yourScope")`

Your filters are now converted to cypher and applied to the executable.

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

_Cypher Query_

```cypher
MATCH (person:Person)
WHERE person.name = 'Yorker Shorton" AND
RETURN person {.name}
```

# Sorting

To use Neo4j sorting you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JSorting();
```

> To use Neo4j Sorting alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4j convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JSorting("yourScope")`.
> You then have to specify this scope on each method you use Neo4j Sorting: `[UseSorting(Scope = "yourScope")]` or `UseSorting(scope = "yourScope")`

Your sorting is now converted to cypher and applied to the executable.

_GraphQL Query:_

```graphql
query GetPersons {
  persons(order: [{ name: ASC }]) {
    name
    addresses {
      street
      city
    }
  }
}
```

_Cypher Query_

```cypher
MATCH (person:Person)
WHERE person.name = 'Yorker Shorton" AND
RETURN person {.name}
```

# Projections

To use Neo4j projections you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JProjections();
```

> To use Neo4j Projections alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4j convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JProjections("yourScope")`.
> You then have to specify this scope on each method you use Neo4j Projections: `[UseProjections(Scope = "yourScope")]` or `UseProjections(scope = "yourScope")`

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

_Cypher Query_

```cypher
MATCH (person:Person)
WHERE person.name = 'Yorker Shorton" AND
RETURN person {.name}
```

# Paging

In order to use pagination with Neo4j, we have to register the Neo4j specific pagination providers.

```csharp
services
    .AddGraphQLServer()
    .AddNeo4JPagingProviders();
```

[Learn more about pagination providers](/docs/hotchocolate/fetching-data/pagination#providers)

## Cursor Pagination

> ⚠️ Note: Not currently supported.

To use cursor based pagination annotate you resolver with `[UsePaging]` or `.UsePaging()`

```csharp
[UseNeo4JDatabase("neo4j")]
[UsePaging]
[UseProjection]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) => new Neo4jExecutable<Person>(session);
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

To use cursor based pagination annotate you resolver with `[UseOffsetPaging]` or `.UseOffsetPaging()`

```csharp
[UseNeo4JDatabase("neo4j")]
[UseOffsetPaging]
[UseProjection]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) => new Neo4jExecutable<Person>(session);
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

_Cypher Query_

```cypher
MATCH (person:Person)
RETURN person SKIP 50 LIMIT 50
```
