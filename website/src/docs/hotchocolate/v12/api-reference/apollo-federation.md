---
title: Apollo Federation Subgraph Support
---

Hot Chocolate includes an implementation of the Apollo Federation v2 specification for creating Apollo Federated subgraphs. Through Apollo Federation, you can combine multiple GraphQL APIs into a single API for your consumers.

[Apollo Federation documentation](https://www.apollographql.com/docs/federation/) provides a robust explanation of the different foundational concepts and principles that are referenced through the rest of this document.

# Get Started
To use the Apollo Federation tools, you need to first install v12.6 or later of the `HotChocolate.ApolloFederation` package.

<PackageInstallation packageName="HotChocolate.ApolloFederation"/>

After installing the necessary package, you'll need to register the Apollo Federation services with the GraphQL server.

```csharp
IServiceCollection services;

services.AddGraphQLServer()
    .AddApolloFederation();
```

# Defining an entity
We'll first need to define an **entity**&mdash;an object type that can resolve its fields across multiple subgraphs. We'll work with a `Product` entity to provide an example of how to do this.

```csharp
public class Product
{
    [GraphQLType(typeof(NonNullType<IdType>))]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }
}
```

Now that we have a type, we need to take the following steps:

1. [Define a key](https://www.apollographql.com/docs/federation/entities#1-define-a-key) for the entity by marking one or more properties with the `[Key]` attribute.
```csharp
public class Product
{
    [GraphQLType(typeof(NonNullType<IdType>))]
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }
}
```

2. [Define an entity reference resolver](https://www.apollographql.com/docs/federation/entities#2-define-a-reference-resolver) by creating a `static` method and annotating it with the `[ReferenceResolver]` attribute. **Note**: if you're using [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), you should make sure the return type is marked as possibly null.
```csharp
public class Product
{
    [GraphQLType(typeof(NonNullType<IdType>))]
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }

    // Note: there is no constraint on the name of the method
    [ReferenceResolver]
    public static async Task<Product?> ResolveReference(
        // Represents the value that would be in the Id property of a Product
        string id,
        // Example of a service that can resolve the Products; a dataloader
        // is recommended to help avoid N+1 queries within the API
        ProductBatchDataLoader dataLoader
    )
    {
        return await dataloader.LoadAsync(id);
    }
}
```

Once the type has a key or keys, and has a reference resolver defined, you can register the type in the GraphQL schema, which will register it as a type within the GraphQL API itself as well as within the [auto-generated `_service { sdl }` field](https://www.apollographql.com/docs/federation/subgraph-spec/#required-resolvers-for-introspection) within the API.

_Entity type registration_
```csharp
services.AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>()
    // other registrations...
    ;

```

_Apollo Federation SDL query_
```graphql
query {
  _service { sdl }
}
```

_Example SDL response_
```json
{
  "data": {
    "_service": {
      "sdl": "type Product @key(fields: \"id\") {\r\n  id: ID!\r\n  name: String!\r\n  price: Float!\r\n}"
    }
  }
}
```
