---
title: Dynamic Schemas
---

In the world of SaaS, one size rarely fits all. With the ever-changing requirements and the need for high flexibility, schemas in a web application often need to be dynamic. In the context of GraphQL, a dynamic schema allows you to adapt the data structure exposed by your API according to varying conditions, be it different tenant, changing data sources, or configuration.

For instance, consider a Content Management System (CMS) where each tenant might require custom fields that are specific to their use case. Having a static GraphQL schema in such a scenario would mean that you need to anticipate all possible custom fields beforehand, which is not practical. A dynamic schema, on the other hand, allows you to add, remove, or modify the types and fields in your schema at runtime based on the specific needs of each tenant. Each tenant can have a different schema, and you can adapt the schema to the tenant's needs without having to redeploy your application.

While creating dynamic schemas in GraphQL offers substantial flexibility, it also comes with its own set of complexities. This is where the `ITypeModule` interface in Hot Chocolate comes into play.

# What is `ITypeModule`?

`ITypeModule` is an interface introduced in Hot Chocolate that allows you to build a component that dynamically provides types to the schema building process.

The `ITypeModule` interface consists of an event `TypesChanged` and a method `CreateTypesAsync`. Here is a brief overview of each:

- `TypesChanged`: This event signals when types have changed and the current schema version needs to be phased out.

- `CreateTypesAsync`: This method is called by the schema building process to create types for a new schema instance. It takes a descriptor context, which provides access to schema building services and conventions, and a cancellation token.

When the underlying structure for a type module changes, for example, due to alterations in a database schema or updates in a JSON file defining types, the `TypesChanged` event can be triggered. This event tells Hot Chocolate to phase out the old schema and introduce a new one with the updated types from the module.

In essence, `ITypeModule` takes care of the complexities associated with providing a dynamic schema with hot-reload functionality, allowing developers to focus on the core logic of their applications.

In the following sections, we'll look at a couple of examples that demonstrate how to use `ITypeModule` to create dynamic schemas in different scenarios.

# Example: Creating Types from a JSON File

In this example, we'll explore how to create a dynamic schema from a JSON file. This scenario might be common if your application allows users to define custom types and fields through a UI, and these definitions are stored as JSON.

Let's consider the following `ITypeModule` implementation:

```csharp
public class JsonTypeModule : ITypeModule
{
    private readonly string _file;

    public JsonTypeModule(string file)
    {
        _file = file;
    }

    public event EventHandler<EventArgs>? TypesChanged;

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        var types = new List<ITypeSystemMember>();

        await using var file = File.OpenRead(_file);
        using var json = await JsonDocument.ParseAsync(file, cancellationToken: cancellationToken);

        foreach (var type in json.RootElement.EnumerateArray())
        {
            var typeDefinition = new ObjectTypeDefinition(type.GetProperty("name").GetString()!);

            foreach (var field in type.GetProperty("fields").EnumerateArray())
            {
                typeDefinition.Fields.Add(
                    new ObjectFieldDefinition(
                        field.GetString()!,
                        type: TypeReference.Parse("String!"),
                        pureResolver: ctx => "foo"));
            }

            types.Add(
                type.GetProperty("extension").GetBoolean()
                    ? ObjectTypeExtension.CreateUnsafe(typeDefinition)
                    : ObjectType.CreateUnsafe(typeDefinition));
        }

        return types;
    }
}
```

In this implementation, `CreateTypesAsync` reads a JSON file, parses it, and creates types based on the content of the JSON. If any of these types are extensions, they are created as such. If the types or their structure change, you could fire the `TypesChanged` event to signal that a new schema needs to be generated.

# Unsafe Type Creation

When working with dynamic schemas and the `ITypeModule` interface, one of the practices you'll encounter is the use of the `CreateUnsafe` method to create types.
The unsafe way to create types, as the name implies, bypasses some of the standard validation logic. This method is useful for advanced scenarios where you need more flexibility, such as when dynamically creating types based on runtime data.

The `CreateUnsafe` method allows you to create types directly from a `TypeDefinition`.

```csharp
var typeDefinition = new ObjectTypeDefinition("DynamicType");
// ... populate typeDefinition ...

var dynamicType = ObjectType.CreateUnsafe(typeDefinition);
```

Using `CreateUnsafe` method for type creation can be a complex task as it involves operating directly on the type definition.
This allows for a lot of flexibility, but it also requires a deeper understanding of the Hot Chocolate type system.

Here are some examples of how you might use the `CreateUnsafe` method to create various types.

> This is by no means an exhaustive list, but it should give you an idea of how to use this feature.

## Creating an Object Type

Let's say we want to create a new object type representing a `Product` in an e-commerce system.
We would start by defining the `ObjectTypeDefinition`:

```csharp
var objectTypeDefinition = new ObjectTypeDefinition("Product")
{
    Description = "Represents a product in the e-commerce system",
    RuntimeType = typeof(Dictionary<string, object>)
};
```

Next, we might want to add fields to this object type. For instance, a `Product` might have an `ID`, `Name`, and `Price`:

```csharp
var idFieldDefinition = new ObjectFieldDefinition(
    "id",
    "Unique identifier for the product",
    TypeReference.Parse("ID!"),
    pureResolver: context => context.Parent<Dictionary<string, object>>()["id"]);

var nameFieldDefinition = new ObjectFieldDefinition(
    "name",
    "Name of the product",
    TypeReference.Parse("String!"),
    pureResolver: context => context.Parent<Dictionary<string, object>>()["name"]);

var priceFieldDefinition = new ObjectFieldDefinition(
    "price",
    "Price of the product",
    TypeReference.Parse("Float!"),
    pureResolver: context => context.Parent<Dictionary<string, object>>()["price"]);

objectTypeDefinition.Fields.Add(idFieldDefinition);
objectTypeDefinition.Fields.Add(nameFieldDefinition);
objectTypeDefinition.Fields.Add(priceFieldDefinition);
```

Here, each resolver retrieves the corresponding value from the parent `Dictionary`.

Next, let's add a field that calculates the price after applying a discount. This field would have an argument specifying the discount percentage:

```csharp
var discountArgument = new ArgumentDefinition(
    "discount",
    "Discount percentage to apply",
    TypeReference.Parse("Float!"));

var discountPriceField = new ObjectFieldDefinition(
    "discountPrice",
    "Price after discount",
    TypeReference.Parse("Float!"),
    pureResolver: context =>
    {
        var product = context.Parent<Dictionary<string, object>>();
        var discountPercentage = context.ArgumentValue<float>("discount");
        var originalPrice = (float) product["price"];
        return originalPrice * (1 - discountPercentage / 100);
    }
)
{
    Arguments = { discountArgument }
};

objectTypeDefinition.Fields.Add(discountPriceField);
```

In this case, the `discountPrice` field takes a `discount` argument and uses it to calculate the discounted price. The resolver retrieves the original price from the parent `Dictionary`, applies the discount, and returns the discounted price.

Finally, we create the `ObjectType` and register it:

```csharp
var productType = ObjectType.CreateUnsafe(objectTypeDefinition);
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    ... // other configuration
    .AddType(productType);
```

Now our `Product` object type has fields `id`, `name`, `price`, and `discountPrice(discount: Float!)`. The `discountPrice` field takes a `discount` argument representing the discount percentage.

## Resolver types

A resolver in Hot Chocolate is a delegate that fetches the data for a specific field. There are two types of resolvers: _async Resolvers_ and _pure Resolvers_.

1. **Async Resolvers**:

   ```csharp
   public delegate ValueTask<object?> FieldResolverDelegate(IResolverContext context);
   ```

   _Async Resolvers_ are are typically async and have access to a `IResolverContext`. They are usually used for fetching data from services or databases.

2. **Pure Resolvers**:

   ```csharp
   public delegate object? PureFieldDelegate(IResolverContext context);
   ```

   _Pure Resolvers_ is used where no side-effects or async calls are needed. All your properties are turned into pure resolvers by Hot Chocolate.
   The execution engine optimizes the execution of these resolvers (through inlining of the value completion) to make it significantly faster.

The decision to use _async Resolvers_ or _pure Resolvers_ depends on your use case. If you need to perform asynchronous operations,or fetch data from services, you would use _async Resolvers_. If your resolver is simply retrieving data without any side effects, _pure Resolvers_ would be a more performant choice.

Let's add a non-pure field resolver to our example. For instance, we can add a `reviews` field that fetches reviews for a product from an external service:

```csharp
var reviewsFieldDefinition = new ObjectFieldDefinition(
    "reviews",
    "Reviews for the product",
    TypeReference.Parse("[Review!]"),
    resolver: async context =>
    {
        var productId = context.Parent<Dictionary<string, object>>()["id"];
        var reviewsService = context.Service<IReviewsService>();
        return await reviewsService.GetReviewsForProduct(productId);
    });

objectTypeDefinition.Fields.Add(reviewsFieldDefinition);
```

Here, `IReviewsService` could be an interface representing a service that fetches reviews. The `reviewsResolver` uses the `Service<T>` method on the `IMiddlewareContext` to retrieve an instance of this service, then calls a method on this service to get the reviews. .

This field resolver is a `FieldResolverDelegate` (i.e., a non-pure resolver) because it needs perform an asynchronous operation.

The resulting schema is:

```graphql
"Represents a product in the e-commerce system"
type Product {
  "Unique identifier for the product"
  id: ID!
  "Name of the product"
  name: String!
  "Price of the product"
  price: Float!
  "Price after discount"
  discountPrice("Discount percentage to apply" discount: Float!): Float!
  "Reviews for the product"
  reviews: String
}
```

## Creating an Input Object Type

Creating an Input Object Type is very similar to creating an Object Type. The major difference lies in the fact that Input Object Types are used in GraphQL mutations or as arguments in queries, whereas Object Types are used in GraphQL queries to define the shape of the returned data. Meaning you don't need to define resolvers for Input Object Types.

An Input Object Type can be created by defining an `InputObjectTypeDefinition` and using the `InputObjectType.CreateUnsafe` method.

Let's create an input object type representing a `ProductInput` which can be used to create or update a product:

```csharp
var inputObjectTypeDefinition = new InputObjectTypeDefinition("ProductInput")
{
    Description = "Represents product input for creating or updating a product",
    RuntimeType = typeof(Dictionary<string, object>)
};

var nameFieldDefinition = new InputFieldDefinition(
    "name",
    "Name of the product",
    TypeReference.Parse("String!"));

var priceFieldDefinition = new InputFieldDefinition(
    "price",
    "Price of the product",
    TypeReference.Parse("Float!"));

inputObjectTypeDefinition.Fields.Add(nameFieldDefinition);
inputObjectTypeDefinition.Fields.Add(priceFieldDefinition);

var productInputType = InputObjectType.CreateUnsafe(inputObjectTypeDefinition);
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    ... // other configuration
    .AddType(productInputType);
```

As with Object Types, you can use the `CreateUnsafe` method to create complex input types based on runtime data.

## Combining the Generated Types

To create a GraphQL mutation, you need an `InputObjectType` to define the input of the mutation and an `ObjectType` to define the output. You can create a mutation by defining a `MutationTypeDefinition` and using the `MutationType.CreateUnsafe` method.

Let's extend the previous examples to create a `createProduct` mutation using the `ProductInput` and `Product` types:

```csharp
var createProductMutationFieldDefinition = new ObjectFieldDefinition(
    "createProduct",
    "Creates a new product",
    TypeReference.Parse("Product!"),
    resolver: async context =>
    {
        var productInput = context.ArgumentValue<Dictionary<string, object>>("input");
        var productService = context.Service<IProductService>();
        var newProduct = await productService.CreateProduct(productInput);
        return newProduct;
    }
)
{
    Arguments =
    {
        new ArgumentDefinition(
            "input",
            "Input for creating the product",
            TypeReference.Parse("ProductInput!"))
    }
};

var mutationTypeDefinition = new ObjectTypeDefinition("Mutation")
{
    RuntimeType = typeof(object)
};

mutationTypeDefinition.Fields.Add(createProductMutationFieldDefinition);

var mutationType = ObjectType.CreateUnsafe(mutationTypeDefinition);
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddMutationType(mutationType)
    ... // other configuration
    .AddType(productInputType)
    .AddType(productType);
```

In this example, we first define a mutation field `createProduct` that takes a `ProductInput` argument and returns a `Product`. The resolver for this field uses a hypothetical `IProductService` to create a new product based on the input.

We then define a `Mutation` type and add the `createProduct` field to it. Finally, we use the `CreateUnsafe` method to create the `Mutation` type and register it along with the `ProductInput` and `Product` types.

With this setup, you can now use the `createProduct` mutation in your GraphQL API:

```graphql
mutation CreateProduct($input: ProductInput!) {
  createProduct(input: $input) {
    id
    name
    price
  }
}
```

With the variable:

```json
{
  "input": {
    "name": "New Product",
    "price": 99.99
  }
}
```

This mutation will create a new product and return its details as a `Product` object.

This way, you can use the generated `InputObjectType` and `ObjectType` together to create a complete GraphQL mutation. Similarly, you can combine other generated types to create the queries, subscriptions, and other parts of your GraphQL schema.
