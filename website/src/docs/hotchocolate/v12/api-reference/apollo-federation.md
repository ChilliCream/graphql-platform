---
title: Apollo Federation Subgraph Support
---
> If you want to read more about Apollo Federation in general, you can head over to the [Apollo Federation documentation](https://www.apollographql.com/docs/federation/), which provides a robust overview and set of examples for this GraphQL architectural pattern. Many of the core principles and concepts are referenced within this document.

Hot Chocolate includes an implementation of the Apollo Federation v2 specification for creating Apollo Federated subgraphs. Through Apollo Federation, you can combine multiple GraphQL APIs into a single API for your consumers.

The documentation describes the syntax for creating an Apollo Federated subgraph using Hot Chocolate and relates the implementation specifics to its counterpart in the Apollo Federation docs. This document *will not* provide a thorough explanation of the Apollo Federation core concepts nor will it describe how you go about creating a supergraph to stitch together various subgraphs, as the Apollo Federation team already provides thorough documentation of those principles.

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

## Define an entity key
Now that we have an object type to work with, we'll [define a key](https://www.apollographql.com/docs/federation/entities#1-define-a-key) for the entity by marking one or more properties with the `[Key]` attribute. This effectively serves as an "identifier" that can uniquely locate an individual record of that type. This will typically be something like a record's primary key, a SKU, or an account number.
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

## Define a reference resolver
Next, we'll need to [define an entity reference resolver](https://www.apollographql.com/docs/federation/entities#2-define-a-reference-resolver) so that the supergraph can resolve data across multiple graphs during a query. A reference resolver is similar to many other [data resolvers in Hot Chocolate](docs/hotchocolate/v12/fetching-data/resolvers) with some key requirements:
1. It must be a `public static` method within the type it is resolving
1. It must be annotated with the `[ReferenceResolver]` attribute
```csharp
public class Product
{
    [GraphQLType(typeof(NonNullType<IdType>))]
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }

    [ReferenceResolver]
    public static async Task<Product?> ResolveReference(
        // Represents the value that would be in the Id property of a Product
        string id,
        // Example of a service that can resolve the Products
        ProductBatchDataLoader dataLoader
    )
    {
        return await dataloader.LoadAsync(id);
    }
}
```

Some important details to highlight about `[ReferenceResolver]` methods.
1. The name of the method decorated with the `[ReferenceResolver]` attribute does not matter. However, as with all programming endeavors, you should aim to provide a descriptive name that reveals the method's intention.
1. The parameter name and type used in the reference resolver **must match** the GraphQL field name of the `[Key]` attribute, e.g., if the GraphQL key field is `id: String!` or `id: ID!` then the reference resolver's parameter must be `string id`.
1. If you're using [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), you should make sure the return type is marked as possibly null.
1. It's recommended to use a [dataloader](/docs/hotchocolate/v12/fetching-data/dataloader) to fetch the data in a reference resolver. This helps the API avoid [an N+1 problem](https://www.apollographql.com/docs/federation/entities-advanced#handling-the-n1-problem) when a query resolves multiple items from a given subgraph.
1. If you have multiple keys defined for an entity, you should include a reference resolver for _each key_ so that the supergraph is able to resolve your entity regardless of which key(s) another graph uses to reference that entity.
```csharp
public class Product
{
    [Key]
    public string Id { get; set; }

    [Key]
    public int Sku { get; set; }

    [ReferenceResolver]
    public static Product? ResolveReferenceById(string id)
    {
        // Locates the Product by its Id.
    }

    [ReferenceResolver]
    public static Product? ResolveReferenceBySku(int sku)
    {
        // Locates the product by SKU
    }
}
```

## Register the entity
Once the type has a key or keys and a reference resolver defined, you can register the type in the GraphQL schema, which will register it as a type within the GraphQL API itself as well as within the [auto-generated `_service { sdl }` field](https://www.apollographql.com/docs/federation/subgraph-spec/#required-resolvers-for-introspection) within the API.

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

## Testing and executing your resolvers
After creating an entity, you'll likely wonder "where is the query field to invoke and test this reference resolver?" The Hot Chocolate engine does the work behind the scenes to connect your `[ReferenceResolver]` method with the [auto-generated `_entites` query](https://www.apollographql.com/docs/federation/subgraph-spec#understanding-query_entities), which serves as the entry point for _all_ entity reference resolvers.

You'll invoke the query by providing an array of representations using a combination of a `__typename` and a `[Key]` field to invoke the appropriate resolver. An example query for our Product would look something like the following.

_Entities query_
```graphql
query {
  _entities(representations: [
    {
      __typename: "Product",
      id: "<id value of the product>"
    }
    # You can provide multiple representations for multiple objects and types in the same query
  ]) {
    ... on Product {
      id
      name
      price
    }
  }
}
```

_Entities query result_
```json
{
  "data": {
    "_entities": [
      {
        "id": "<id value of the product>",
        "name": "Foobar",
        "price": 10.99
      }
      // Any other values that were found, or null
    ]
  }
}
```

> **Note**: The `_entities` query is an internal detail that is necessary for the supergraph to properly resolve data. API consumers **should not** use an `_entities` query directly, nor should they use a subgraph directly. We're highlighting how to use the `_entities` so that you have the knowledge to validate your API at runtime or using tools like [`Microsoft.AspNetCore.Mvc.Testing`](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0).

# Extending an entity type
TODO
