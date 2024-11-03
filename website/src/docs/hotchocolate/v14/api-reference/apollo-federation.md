---
title: Apollo Federation Subgraph Support
---

> If you want to read more about Apollo Federation in general, you can head over to the [Apollo Federation documentation](https://www.apollographql.com/docs/federation/), which provides a robust overview and set of examples for this GraphQL architectural pattern. Many of the core principles and concepts are referenced within this document.

Hot Chocolate includes an implementation of the Apollo Federation v1 specification for creating Apollo Federated subgraphs. Through Apollo Federation, you can combine multiple GraphQL APIs into a single API for your consumers.

The documentation describes the syntax for creating an Apollo Federated subgraph using Hot Chocolate and relates the implementation specifics to its counterpart in the Apollo Federation docs. This document _will not_ provide a thorough explanation of the Apollo Federation core concepts nor will it describe how you go about creating a supergraph to stitch together various subgraphs, as the Apollo Federation team already provides thorough documentation of those principles.

You can find example projects of the Apollo Federation library in [Hot Chocolate examples](https://github.com/ChilliCream/graphql-platform/tree/main/src/HotChocolate/ApolloFederation/examples).

# Get Started

To use the Apollo Federation tools, you need to first install v12.6 or later of the `HotChocolate.ApolloFederation` package.

<PackageInstallation packageName="HotChocolate.ApolloFederation"/>

After installing the necessary package, you'll need to register the Apollo Federation services with the GraphQL server.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation();
```

# Defining an entity

Now that the API is ready to support Apollo Federation, we'll need to define an **entity**&mdash;an object type that can resolve its fields across multiple subgraphs. We'll work with a `Product` entity to provide an example of how to do this.

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

## Define an entity key

Once we have an object type to work with, we'll [define a key](https://www.apollographql.com/docs/federation/entities#1-define-a-key) for the entity. A key in an Apollo Federated subgraph effectively serves as an "identifier" that can uniquely locate an individual record of that type. This will typically be something like a record's primary key, a SKU, or an account number.

<ExampleTabs>

<Implementation>

In an implementation-first approach, we'll use the `[Key]` attribute on any property or properties that can be referenced as a key by another subgraph.

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

In a code-first approach, we'll use the `Key()` method to designate any GraphQL fields that can be reference as a key by another subgraph.

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

        // Matches the Id property when it is converted to the GraphQL schema
        descriptor.Key("id");
    }
}
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

## Define a reference resolver

Next, we'll need to define an [entity reference resolver](https://www.apollographql.com/docs/federation/entities#2-define-a-reference-resolver) so that the supergraph can resolve this entity across multiple subgraphs during a query. Every subgraph that contributes at least one unique field to an entity must define a reference resolver for that entity.

<ExampleTabs>

<Implementation>

In an implementation-first approach, a reference resolver will work just like a [regular resolver](/docs/hotchocolate/v14/fetching-data/resolvers) with some key differences:

1. It must be annotated with the `[ReferenceResolver]` attribute
1. It must be a `public static` method _within_ the type it is resolving

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
1. If you're using [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), you should make sure the return type is marked as possibly null, i.e., `T?`.
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

</Implementation>

<Code>

We'll now chain a `ResolveReferenceWith()` method call off of the `Key()` method call from the previous step. This will create a [resolver](/docs/hotchocolate/v14/fetching-data/resolvers) that the Hot Chocolate engine can invoke.

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

        descriptor.Key("id")
            .ResolveReferenceWith(_ => ResolveByIdAsync(default!, default!));
    }

    private static Task<Product?> ResolveByIdAsync(
        // Represents the value that would be in the Id property of a Product
        string id,
        // Example of a service that can resolve the Products
        ProductBatchDataLoader dataLoader)
    {
        return await dataLoader.LoadAsync(id);
    }
}
```

Some important details to highlight about entity reference resolvers.

1. The parameter name and type used in the reference resolver **must match** the GraphQL field name of the `Key()` field set, e.g., if the GraphQL key field is `id: String!` or `id: ID!` then the reference resolver's parameter must be `string id`.
1. If you're using [nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), you should make sure the return type is marked as possibly null, i.e., `T?`.
1. For each call to the `Key()` method, you should include a reference resolver so that the supergraph is able to resolve your entity regardless of which key(s) another graph uses to reference that entity.

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Key("id")
            .ResolveReferenceWith(_ => ResolveByIdAsync(default!));

        descriptor.Key("sku")
            .ResolveReferenceWith(_ => ResolveBySkuAsync(default!))
    }

    private static Task<Product?> ResolveByIdAsync(string id)
    {
        // Locate the product by its Id
    }

    private static Task<Product?> ResolveBySkuAsync(default!)
    {
        // Locate the product by its SKU instead
    }
}
```

</Code>

<Schema>

**Coming soon**

</Schema>
</ExampleTabs>

> ### A note about reference resolvers
>
> It's recommended to use a [dataloader](/docs/hotchocolate/v14/fetching-data/dataloader) to fetch the data in a reference resolver. This helps the API avoid [an N+1 problem](https://www.apollographql.com/docs/federation/entities-advanced#handling-the-n1-problem) when a query resolves multiple items from a given subgraph.

## Register the entity

After our type has a key or keys and a reference resolver defined, you'll register the type in the GraphQL schema, which will register it as a type within the GraphQL API itself as well as within the [auto-generated `_service { sdl }` field](https://www.apollographql.com/docs/federation/subgraph-spec/#required-resolvers-for-introspection) within the API.

_Entity type registration_

<ExampleTabs>

<Implementation>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>()
    // other registrations...
    ;
```

</Implementation>

<Code>

```csharp
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<ProductType>()
    // other registrations...
    ;
```

</Code>

<Schema>

**Coming soon**

</Schema>
</ExampleTabs>

## Testing and executing your reference resolvers

After creating an entity, you'll likely wonder "how do I invoke and test this reference resolver?" Entities that define a reference resolver can be queried through the [auto-generated `_entities` query](https://www.apollographql.com/docs/federation/subgraph-spec#understanding-query_entities) at the subgraph level.

You'll invoke the query by providing an array of representations using a combination of a `__typename` and key field values to invoke the appropriate resolver. An example query for our `Product` would look something like the following.

_Entities query_

```graphql
query {
  _entities(
    representations: [
      { __typename: "Product", id: "<id value of the product>" }
      # You can provide multiple representations for multiple objects and types in the same query
    ]
  ) {
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

> **Note**: The `_entities` field is an internal implementation detail of Apollo Federation that is necessary for the supergraph to properly resolve entities. API consumers **should not** use the `_entities` field directly nor should they send requests to a subgraph directly. We're only highlighting how to use the `_entities` field so that you can validate and test your subgraph and its entity reference resolvers at runtime or using tools like [`Microsoft.AspNetCore.Mvc.Testing`](https://learn.microsoft.com/aspnet/core/test/integration-tests).

# Referencing an entity type

Now that we have an entity defined in one of our subgraphs, let's go ahead and create a second subgraph that will make use of our `Product` type. Remember, all of this work should be performed in a _**separate API project**_.

In the second subgraph, we'll create a `Review` type that is focused on providing reviews of `Product` entities from the other subgraph. We'll do that by defining our `Review` type along with a [service type reference](https://www.apollographql.com/docs/federation/entities/#referencing-an-entity-without-contributing-fields) that represents the `Product`.

In our new subgraph API we'll need to start by creating the `Product`. When creating the extended service type, make sure to consider the following details

- The _GraphQL type name_ **must match**. Often, this can be accomplished by using the same class name between the projects, but you can also use tools like the `[GraphQLName(string)]` attribute or `IObjectTypeDescriptor<T>.Name(string)` method to explicitly set a GraphQL name.
- The extended type must include _at least one_ key that matches in both name and GraphQL type from the source graph.
  - In our example, we'll be referencing the `id: ID!` field that was defined on our `Product`

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

// In your Program
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>();
```

</Implementation>

<Code>

```csharp
public class Product
{
    public string Id { get; set; }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();

        descriptor.Key("id");
        descriptor.Field(product => product.Id).ID();
    }
}

// In your Program
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

Next, we'll create our `Review` type that has a reference to the `Product` entity. Similar to our first class, we'll need to denote the type's key(s) and the corresponding entity reference resolver(s).

<ExampleTabs>

<Implementation>

```csharp
public class Review
{
    [ID]
    [Key]
    public string Id { get; set; }

    public string Content { get; set; }

    [GraphQLIgnore]
    public string ProductId { get; set; }

    public Product GetProduct() => new Product { Id = ProductId };

    [ReferenceResolver]
    public static Review? ResolveReference(string id)
    {
        // Omitted for brevity; some kind of service to retrieve the review.
    }
}

// In your Program
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<Product>()
    .AddType<Review>();
```

</Implementation>

<Code>

```csharp
public class Review
{
    public string Id { get; set; }

    public string Content { get; set; }

    public string ProductId { get; set; }

    public Product GetProduct() => new Product { Id = ProductId };
}

public class ReviewType : ObjectType<Review>
{
    protected override void Configure(IObjectTypeDescriptor<Review> descriptor)
    {
        descriptor.Key("id").ResolveReferenceWith(_ => ResolveReviewById(default!));
        descriptor.Field(review => review.Id).ID();

        descriptor.Ignore(review => review.ProductId);
    }

    private static Review? ResolveReviewById(string id)
    {
        // Omitted for brevity
    }
}

// In your Program
builder.Services
    .AddGraphQLServer()
    .AddApolloFederation()
    .AddType<ProductType>()
    .AddType<ReviewType>();
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

In the above snippet two things may pop out as strange to you:

1. Why did we explicitly ignore the `ProductId` property?
   - The `ProductId` is, in essence, a "foreign key" to the other graph. Instead of presenting that data as a field of the `Review` type, we're presenting it through the `product: Product!` GraphQL field that is produced by the `GetProduct()` method. This allows the Apollo supergraph to stitch the `Review` and `Product` types together and represent that a query can traverse from the `Review` to the `Product` it is reviewing and make the API more graph-like. With that said, it is not strictly necessary to ignore the `ProductId` or any other external entity Id property.
2. Why does the `GetProduct()` method instantiate its own `new Product { Id = ProductId }` object?
   - Since our goal with Apollo Federation is decomposition and [concern-based separation](https://www.apollographql.com/docs/federation/#concern-based-separation), a second subgraph is likely to have that "foreign key" reference to the type that is reference from the other subgraph. However, this graph does not "own" the actual data of the entity itself. This is why our sample simply performs a `new Product { Id = ProductId }` statement for the resolver: it's not opinionated about how the other data of a `Product` is resolved from its owning graph.

With our above changes, we can successfully connect these two subgraphs into a single query within an Apollo supergraph, allowing our API users to send a query like the following.

```graphql
query {
  # Example - not explicitly defined in our tutorial
  review(id: "<review id>") {
    id
    content
    product {
      id
      name
    }
  }
}
```

As a reminder, you can create and configure a supergraph by following either the [Apollo Router documentation](https://www.apollographql.com/docs/router/quickstart/) or [`@apollo/gateway` documentation](https://www.npmjs.com/package/@apollo/gateway).

## Contributing fields through resolvers

Now that our new subgraph has the `Product` reference we can [contribute additional fields to the type](https://www.apollographql.com/docs/federation/entities#contributing-entity-fields). Similar to other types in Hot Chocolate, you can create new fields by defining different method or property resolvers. For a full set of details and examples on creating resolvers, you can read our [documentation on resolvers](/docs/hotchocolate/v14/fetching-data/resolvers).

For now, we'll focus on giving our supergraph the ability to retrieve all reviews for a given product by adding a `reviews: [Review!]!` property to the type.

<ExampleTabs>

<Implementation>

```csharp
[ExtendServiceType]
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }

    public async Task<IEnumerable<Review>> GetReviews(
        ReviewRepository repo // example of how you might resolve this data
    )
    {
        return await repo.GetReviewsByProductIdAsync(Id);
    }
}
```

</Implementation>

<Code>

```csharp
public class Product
{
    public string Id { get; set; }

    public async Task<IEnumerable<Review>> GetReviews(
        ReviewRepository repo // example of how you might resolve this data
    )
    {
        return await repo.GetReviewsByProductIdAsync(Id);
    }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();

        descriptor.Key("id");
        descriptor.Field(product => product.Id).ID();
    }
}
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

These changes will successfully add the new field within the subgraph! However, our current implementation cannot be resolved if we start at a product such as `query { product(id: "foo") { reviews { ... } } }`. To fix this, we'll need to implement an entity reference resolver in our second subgraph.

As mentioned above, since this subgraph does not "own" the data for a `Product`, our resolver will be fairly naive, similar to the `Review::GetProduct()` method: it will simply instantiate a `new Product { Id = id }`. We do this because the reference resolver should only be directly invoked by the supergraph, so our new reference resolver will simply assume the data exists. However, if there is data that needs to be fetched from some kind of data store, the resolver can still do this just as any other data resolver in Hot Chocolate.

<ExampleTabs>

<Implementation>

```csharp
[ExtendServiceType]
public class Product
{
    [ID]
    [Key]
    public string Id { get; set; }

    public async Task<IEnumerable<Review>> GetReviews(
        ReviewRepository repo // example of how you might resolve this data
    )
    {
        return await repo.GetReviewsByProductIdAsync(Id);
    }

    [ReferenceResolver]
    public static Product ResolveProductReference(string id) => new Product { Id = id };
}
```

</Implementation>

<Code>

```csharp
public class Product
{
    public string Id { get; set; }

    public async Task<IEnumerable<Review>> GetReviews(
        ReviewRepository repo // example of how you might resolve this data
    )
    {
        return await repo.GetReviewsByProductIdAsync(Id);
    }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();

        descriptor.Key("id").ResolveReferenceWith(_ => ResolveProductReference(default!));
        descriptor.Field(product => product.Id).ID();
    }

    private static Product ResolveProductReference(string id) => new Product { Id = id };
}
```

</Code>

<Schema>

**Coming soon**

</Schema>

</ExampleTabs>

With the above changes, our supergraph can now support traversing both "from a review to a product" as well as "from a product to a review"!

```graphql
# Example root query fields - not implemented in the tutorial
query {
  # From a review to a product (back to the reviews)
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
  # From a product to a review
  product(id: "bar") {
    id
    name
    price
    reviews {
      id
      content
    }
  }
}
```
