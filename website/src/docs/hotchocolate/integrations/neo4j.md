---
title: Neo4J Database
---

## Requirements
1. [Neo4J Database](https://neo4j.com/download/) v4.1.0 and above.
2. [APOC](https://github.com/neo4j-contrib/neo4j-apoc-procedures) Awesome Procedures On Cypher v4.1.0 and above.

# What does this integration do?

It is a GraphQL to openCypher execution layer for HotChocolate. It makes it easy for developers to user Neo4J and GraphQL together. With this integration, you can translate paging, filtering, sorting, and projections, directly into native cypher queries.

You can find a full example project in Github [here](https://github.com/ChilliCream/graphql-workshop-neo4j) this example covers code first and schema first.

Lets get started with a simple tutorial.

# Step 1: Launch Neo4J Database

Run Neo4J in Docker

```bash
docker run \
    -p 7474:7474 -p 7687:7687 \
    -v $PWD/data:/data -v $PWD/plugins:/plugins \
    --name neo4j-apoc \
    -e NEO4J_apoc_export_file_enabled=true \
    -e NEO4J_apoc_import_file_enabled=true \
    -e NEO4J_apoc_import_file_use__neo4j__config=true \
    -e NEO4JLABS_PLUGINS=\[\"apoc\"\] \
    neo4j
```
or download and install Neo4J Desktop.



# Step 1: Create a GraphQL server project

Open your preferred terminal and select a directory where you want to add the code of this tutorial.

1. Create an empty ASP.NET Core server project.

```bash
dotnet new web -n Demo
```

To use the Neo4J integration, you need to install the package `HotChocolate.Data.Neo4J`.

```bash
dotnet add ./Demo package HotChocolate.Data.Neo4J
```

# Neo4JExecutable

The whole integration builds around `IExecutable<T>`.
The execution engine picks up the `IExecutable` and executes it efficiently.

```csharp
[UseNeo4JDatabase("neo4j")]
[UsePaging]
[UseProjection]
[UseSorting]
[UseFiltering]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) => new(session);
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

To use Neo4J sorting you need to register the convention on the schema builder:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNeo4JSorting();
```

> To use Neo4J Sorting alongside with `IQueryable`/`IEnumerable`, you have to register the Neo4J convention under a different scope.
> You can specify the scope on the schema builder by executing `AddNeo4JSorting("yourScope")`.
> You then have to specify this scope on each method you use MongoDb Sorting: `[UseSorting(Scope = "yourScope")]` or `UseSorting(scope = "yourScope")`

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
> You then have to specify this scope on each method you use MongoDb Projections: `[UseProjections(Scope = "yourScope")]` or `UseProjections(scope = "yourScope")`

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

The integration comes with providers for offset and cursor-based pagination

## Cursor Pagination

To use cursor based pagination annoate you resolver with `[UseNeo4JPaging]` or `.UseNeo4JPaging()`

```csharp
[UseNeo4JDatabase("neo4j")]
[UsePaging]
[UseProjection]
public IExecutable<Person> GetPersons([ScopedService] IAsyncSession session) => new(session);
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

To use cursor based pagination annotate your resolver with `[UseNeo4JPaging]` or `.UseNeo4JPaging()`

<ExampleTabs>
<ExampleTabs.Annotation>


```csharp
[UseNeo4JDatabase("neo4j")]
[UseOffsetPaging]
[UseProjection]
public IExecutable<Person> GetPersons(
  [ScopedService] IAsyncSession session) => new(session);
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp

```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```graphql

```

</ExampleTabs.Schema>
</ExampleTabs>


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
