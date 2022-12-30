---
title: Neo4J Database
---

HotChocolate has a data integration for Neo4J.
With this integration, you can translate paging, filtering, sorting, and projections, directly into native cypher queries.

You can find a example project in [HotChocolate Examples](https://github.com/ChilliCream/graphql-workshop-neo4j)

# Get Started

To use the Neo4J integration, you need to install the package `HotChocolate.Data.Neo4J`.

<PackageInstallation packageName="HotChocolate.Data.Neo4J" />

# Neo4JExecutable

The whole integration builds around `IExecutable<T>`.
The execution engine picks up the `IExecutable` and executes it efficiently.

```csharp
[UseNeo4JDatabase("neo4j")]
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) =>
    new Neo4JExecutable<Person>(session);
```

# Filtering

To use Neo4J filtering you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JFiltering();
```

> To use Neo4J filtering alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4J convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JFiltering("yourScope")`.
> You then have to specify this scope on each method you use Neo4J filtering: `[UseFiltering(Scope = "yourScope")]` or `UseFiltering(scope = "yourScope")`

Your filters are now converted to cypher and applied to the executable.

_GraphQL Query:_

```graphql
query GetPersons {
  persons(where: { name: { eq: "Yorker Shorton" } }) {
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
WHERE person.name = 'Yorker Shorton"
RETURN person {.name}
```

# Sorting

To use Neo4J sorting you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JSorting();
```

> To use Neo4J Sorting alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4J convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JSorting("yourScope")`.
> You then have to specify this scope on each method you use Neo4J Sorting: `[UseSorting(Scope = "yourScope")]` or `UseSorting(scope = "yourScope")`

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

To use Neo4J projections you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JProjections();
```

> To use Neo4J Projections alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4J convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JProjections("yourScope")`.
> You then have to specify this scope on each method you use Neo4J Projections: `[UseProjections(Scope = "yourScope")]` or `UseProjections(scope = "yourScope")`

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

In order to use pagination with Neo4J, we have to register the Neo4J specific pagination providers.

```csharp
services
    .AddGraphQLServer()
    .AddNeo4JPagingProviders();
```

[Learn more about pagination providers](/docs/hotchocolate/v13/fetching-data/pagination#providers)

## Cursor Pagination

Not Implemented!

## Offset Pagination

To use offset based pagination annotate your resolver with `[UseOffsetPaging]` or `.UseNeo4JPaging()`

```csharp
[UseNeo4JDatabase("neo4j")]
[UseOffsetPaging]
[UseProjection]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) =>
    new Neo4JExecutable<Person>(session);
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
