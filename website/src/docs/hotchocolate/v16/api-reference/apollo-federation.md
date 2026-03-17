---
title: Apollo Federation Subgraph Support
description: Learn how to create Apollo Federated subgraphs using Hot Chocolate v16.
---

> For more about Apollo Federation concepts, see the [Apollo Federation documentation](https://www.apollographql.com/docs/federation/). Many of the core principles referenced here are documented there.

Hot Chocolate includes an implementation of the Apollo Federation v1 specification for creating Apollo Federated subgraphs. Through Apollo Federation, you can combine multiple GraphQL APIs into a single API for your consumers.

This page describes the syntax for creating an Apollo Federated subgraph using Hot Chocolate and relates the implementation specifics to their counterpart in the Apollo Federation docs. It does not provide a thorough explanation of Apollo Federation core concepts or describe how you create a supergraph to stitch together subgraphs.

You can find example projects in [Hot Chocolate examples](https://github.com/ChilliCream/graphql-platform/tree/main/src/HotChocolate/ApolloFederation/examples).

# Get Started

Install the `HotChocolate.ApolloFederation` package:

<PackageInstallation packageName="HotChocolate.ApolloFederation"/>

Register the Apollo Federation services:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation();
```

# Defining an Entity

An **entity** is an object type that can resolve its fields across multiple subgraphs. The following examples use a `Product` entity.

<ExampleTabs>
<Implementation>

```csharp
public class Product
{
    [ID]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }
}
```

</Implementation>

<Code>

```csharp
public class Product
{
      public string Id { get; set; }

      public string Name { get; set; }

      public float Price { get; set; }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(product => product.Id).ID();
    }
}
```

</Code>

<Schema>

**Coming soon**

</Schema>
</ExampleTabs>

## Define an Entity Key

[Define a key](https://www.apollographql.com/docs/federation/entities#1-define-a-key) for the entity. A key serves as an identifier that uniquely locates an individual record. This is typically something like a primary key, SKU, or account number.

<ExampleTabs>

<Implementation>

Use the `[Key]` attribute on the property or properties that serve as the key:

```csharp
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }
}
```

</Implementation>

<Code>

Use the `Key()` method on the descriptor:

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(product => product.Id).ID();
        descriptor.Key("id");
    }
}
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

## Define a Reference Resolver

Define an [entity reference resolver](https://www.apollographql.com/docs/federation/entities#2-define-a-reference-resolver) so that the supergraph can resolve the entity across subgraphs. Every subgraph that contributes at least one unique field to an entity must define a reference resolver.

<ExampleTabs>

<Implementation>

A reference resolver must be annotated with `[ReferenceResolver]` and must be a `public static` method within the type it resolves:

```csharp
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    public float Price { get; set; }

    [ReferenceResolver]
    public static async Task<Product?> ResolveReference(
        string id,
        ProductBatchDataLoader dataLoader)
    {
        return await dataLoader.LoadAsync(id);
    }
}
```

Key details about `[ReferenceResolver]` methods:

1. The method name does not matter, but choose a descriptive one.
2. The parameter name and type must match the GraphQL field name of the `[Key]` attribute. For example, if the key field is `id: ID!`, the parameter must be `string id`.
3. Mark the return type as nullable (`T?`) when using nullable reference types.
4. If you define multiple keys, include a reference resolver for each one.

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
        // Locate the Product by its Id
    }

    [ReferenceResolver]
    public static Product? ResolveReferenceBySku(int sku)
    {
        // Locate the product by SKU
    }
}
```

</Implementation>

<Code>

Chain `ResolveReferenceWith()` off of the `Key()` method:

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Field(product => product.Id).ID();

        descriptor.Key("id")
            .ResolveReferenceWith(_ => ResolveByIdAsync(default!, default!));
    }

    private static Task<Product?> ResolveByIdAsync(
        string id,
        ProductBatchDataLoader dataLoader)
    {
        return dataLoader.LoadAsync(id);
    }
}
```

Key details:

1. The parameter name and type must match the GraphQL field name of the `Key()` field set.
2. Mark the return type as nullable (`T?`).
3. Include a reference resolver for each call to `Key()`.

</Code>

<Schema>

**Coming soon**

</Schema>
</ExampleTabs>

> We recommend using a [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader) in reference resolvers. This helps avoid [the N+1 problem](https://www.apollographql.com/docs/federation/entities-advanced#handling-the-n1-problem).

## Register the Entity

Register the type in the GraphQL schema:

<ExampleTabs>

<Implementation>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>();
```

</Implementation>

<Code>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<ProductType>();
```

</Code>

<Schema>

**Coming soon**

</Schema>
</ExampleTabs>

## Testing Reference Resolvers

Entities with a reference resolver can be queried through the auto-generated `_entities` query at the subgraph level:

```graphql
query {
  _entities(representations: [{ __typename: "Product", id: "<id value>" }]) {
    ... on Product {
      id
      name
      price
    }
  }
}
```

> The `_entities` field is an internal implementation detail of Apollo Federation. API consumers should not use it directly. It is documented here for testing and validation purposes.

# Referencing an Entity Type

To reference an entity defined in another subgraph, create a service type reference in a separate API project. The GraphQL type name must match, and the extended type must include at least one key that matches the source graph.

<ExampleTabs>

<Implementation>

```csharp
[ExtendServiceType]
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }
}

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>();
```

</Implementation>

<Code>

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();
        descriptor.Key("id");
        descriptor.Field(product => product.Id).ID();
    }
}

builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<ProductType>();
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

Create a type (e.g., `Review`) that references the entity, and add a reference resolver so the supergraph can traverse between the types:

<ExampleTabs>

<Implementation>

```csharp
[ExtendServiceType]
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }

    public async Task<IEnumerable<Review>> GetReviews(ReviewRepository repo)
    {
        return await repo.GetReviewsByProductIdAsync(Id);
    }

    [ReferenceResolver]
    public static Product ResolveProductReference(string id) =>
        new Product { Id = id };
}
```

</Implementation>

<Code>

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();
        descriptor.Key("id")
            .ResolveReferenceWith(_ => ResolveProductReference(default!));
        descriptor.Field(product => product.Id).ID();
    }

    private static Product ResolveProductReference(string id) =>
        new Product { Id = id };
}
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

With these changes, the supergraph supports traversing from a review to a product and from a product to its reviews:

```graphql
query {
  review(id: "foo") {
    id
    content
    product {
      id
      name
      price
      reviews {
        id
        content
      }
    }
  }
}
```

For creating a supergraph, see the [Apollo Router documentation](https://www.apollographql.com/docs/router/quickstart/) or [`@apollo/gateway` documentation](https://www.npmjs.com/package/@apollo/gateway).

# Troubleshooting

**"\_entities" query returns null for a representation**
Verify that the `__typename` value matches the GraphQL type name exactly and that the key field names and types match the entity definition.

**Reference resolver parameter mismatch**
The parameter name in the reference resolver must match the GraphQL field name (not the C# property name). For example, if the GraphQL field is `id`, the parameter must be `string id`.

**Entity type name does not match across subgraphs**
Use `[GraphQLName("...")]` or `descriptor.Name("...")` to set the GraphQL name explicitly if the C# class names differ.

# Next Steps

- [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers) for resolver patterns
- [DataLoader](/docs/hotchocolate/v16/fetching-data/dataloader) for batching in reference resolvers
- [Apollo Federation docs](https://www.apollographql.com/docs/federation/) for supergraph configuration
