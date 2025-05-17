---
title: "Relay"
---

> Note: Even though they originated in Relay, the design principles described in this document are not exclusive to Relay. They lead to an overall better schema design, which is why we recommend them to **all** users of Hot Chocolate.

[Relay](https://relay.dev) is a JavaScript framework for building data-driven React applications with GraphQL, which is developed and used by _Facebook_.

As part of a specification Relay proposes some schema design principles for GraphQL servers in order to more efficiently fetch, refetch and cache entities on the client. In order to get the most performance out of Relay our GraphQL server needs to abide by these principles.

[Learn more about the Relay GraphQL Server Specification](https://relay.dev/docs/guides/graphql-server-specification)

<Video videoId="qWguoAMzn9E" />

# Global identifiers

If an output type contains an `id: ID!` field, [Relay](https://relay.dev) and other GraphQL clients will consider this the unique identifier of the entity and might use it to construct a flat cache. This can be problematic, since we could have the same identifier for two of our types. When using a database for example, a `Foo` and `Bar` entity could both contain a row with the identifier `1` in their respective tables.

We could try and enforce unique identifiers for our Ids. Still, as soon as we introduce another data source to our schema, we might be facing identifier collisions between entities of our various data sources.

Fortunately there is an easier, more integrated way to go about solving this problem in Hot Chocolate: Global identifiers.

With Global Identifiers, Hot Chocolate adds a middleware that automatically serializes our identifiers to be unique within the schema. The concern of globally unique identifiers is therefore kept separate from our business domain and we can continue using the "real" identifiers within our business code, without worrying about uniqueness for a client.

## Usage in Output Types

Id fields can be opted in to the global identifier behavior using the `ID` middleware.

Hot Chocolate automatically combines the value of fields annotated as `ID` with another value to form a global identifier. Per default, this additional value is the name of the type the Id belongs to. Since type names are unique within a schema, this ensures that we are returning a unique Id within the schema. If our GraphQL server serves multiple schemas, the schema name is also included in this combined Id. The resulting Id is then Base64 encoded to make it opaque.

<ExampleTabs>
<Implementation>

```csharp
public class Product
{
    [ID]
    public int Id { get; set; }
}
```

If no arguments are passed to the `[ID]` attribute, it will use the name of the output type, in this case `Product`, to serialize the Id.

The `[ID]` attribute can be used on primary key fields and on fields that act as foreign keys. For these, we have to specify the name of the type they are referencing manually. In the below example, a type named `Foo` is being referenced using its Id.

```csharp
[ID("Foo")]
public int FooId { get; set; }
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
        descriptor.Field(f => f.Id).ID();
    }
}
```

If no arguments are passed to `ID()`, it will use the name of the output type, in this case `Product`, to serialize the Id.

The `ID()` can not only be used on primary key fields but also on fields that act as foreign keys. For these, we have to specify the name of the type they are referencing manually. In the below example, a type named `Foo` is being referenced using its Id.

```csharp
descriptor.Field(f => f.FooId).ID("Foo");
```

</Code>
<Schema>

The approach of either implementation-first or code-first can be used in conjunction with schema-first.

</Schema>
</ExampleTabs>

The type of fields specified as `ID` is also automatically rewritten to the ID scalar.

[Learn more about the ID scalar](/docs/hotchocolate/v15/defining-a-schema/scalars#id)

## Usage in Input Types

If our `Product` output type returns a serialized Id, all arguments and fields on input object types, accepting a `Product` Id, need to be able to interpret the serialized Id.
Therefore we also need to define them as `ID`, in order to deserialize the serialized Id to the actual Id.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    public Product GetProduct([ID] int id)
    {
        // Omitted code for brevity
    }
}
```

In input object types we can use the `[ID]` attribute on specific fields.

```csharp
public class ProductInput
{
    [ID]
    public int ProductId { get; set; }
}
```

Per default all serialized Ids are accepted. If we want to only accept Ids that have been serialized for the `Product` output type, we can specify the type name as argument to the `[ID]` attribute.

```csharp
public Product GetProduct([ID(nameof(Product))] int id)
```

This will result in an error if an Id, serialized using a different type name than `Product`, is used as input.

</Implementation>
<Code>

```csharp
descriptor
    .Field("product")
    .Argument("id", a => a.Type<NonNullType<IdType>>().ID())
    .Type<ProductType>()
    .Resolve(context =>
    {
        var id = context.ArgumentValue<int>("id");

        // Omitted code for brevity
    });
```

> Note: `ID()` can only be used on fields and arguments with a concrete type. Otherwise type modifiers like non-null or list can not be correctly rewritten.

In input object types we can use `ID()` on specific fields.

```csharp
descriptor
    .Field("id")
    .Type<NonNullType<IdType>>()
    .ID();
```

Per default all serialized Ids are accepted. If we want to only accept Ids that have been serialized for the `Product` output type, we can specify the type name as argument to `ID()`.

```csharp
.Argument("id", a => a.Type<NonNullType<IdType>>().ID(nameof(Product)))
```

This will result in an error if an Id, serialized using a different type name than `Product`, is used as input.

</Code>
<Schema>

The approach of either implementation-first or code-first can be used in conjunction with schema-first.

</Schema>
</ExampleTabs>

## Id Serializer

Unique (or global) Ids are generated using the `IIdSerializer`. We can access it like any other service and use it to serialize or deserialize global Ids ourselves.

```csharp
public class Query
{
    public string Example(IIdSerializer serializer)
    {
        string serializedId = serializer.Serialize(null, "Product", "123");

        IdValue deserializedIdValue = serializer.Deserialize(serializedId);
        object deserializedId = deserializedIdValue.Value;

        // Omitted code for brevity
    }
}
```

The `Serialize()` method takes the schema name as a first argument, followed by the type name and lastly the actual Id.

[Learn more about accessing services](/docs/hotchocolate/v15/fetching-data/resolvers#injecting-services)

# Complex Ids

In certain situations, you may need to use complex identifiers for your data models, rather than simple integers or strings. HotChocolate provides support for complex IDs by allowing you to define custom ID types, which can be used in your GraphQL schema.

## Defining Complex ID

To define a complex ID, you need to create a new class or struct that will represent the complex ID, and use the `[ID]` attribute in the corresponding data model class. In this example, we will create a complex ID for a `Product` class.

```csharp
public class Product
{
    [ID] // Define the ID on the type
    public ProductId Id { get; set; }
}
```

### Using Type Extensions for Complex ID

If your `Product` model does not have an ID field, but you still want to use a complex ID for GraphQL queries, you can use a type extension.

A type extension allows you to add fields to a type that are only available within the GraphQL schema, without modifying the actual data model.
Here's how you can define the type extension:

```csharp
[ExtendObjectType(typeof(Product))]
public class ProductExtensions
{
    // Define a method that will be used to compute the complex ID
    [ID<Product>]
    public ProductId GetId([Parent] Product product)
        => new ProductId(product.SKU, product.BatchNumber);
}
```

This approach allows you to use complex IDs in your GraphQL schema without needing to modify your data models. It's particularly useful when working with databases that use **compound primary keys**, as it allows you to represent these keys as complex IDs in your GraphQL schema.

## Creating Complex ID Structs

Here's how you can define the `ProductId` struct:

```csharp
public readonly record struct ProductId(string SKU, int BatchNumber)
{
    // Override ToString to provide a string representation for the complex ID
    public override string ToString() => $"{SKU}:{BatchNumber}";

    // Create a Parse method that converts a string representation back to the complex ID
    public static ProductId Parse(string value)
    {
        var parts = value.Split(':');
        return new ProductId(parts[0], int.Parse(parts[1]));
    }
}
```

This struct has a string `SKU` and an integer `BatchNumber` property, and can be converted to and from a string for easy usage in GraphQL queries.

## Configuring Type Converters

To integrate the `ProductId` struct into HotChocolate's type system, you need to define type converters. These converters enable HotChocolate to automatically transform between the `ProductId` struct and a string representation.

```csharp
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    // Add a type converter from string to your complex ID type
    .AddTypeConverter<string, ProductId>(ProductId.Parse)
    // Add a type converter back to string
    .AddTypeConverter<ProductId, string>(x => x.ToString())
    // Enable global object identification
    .AddGlobalObjectIdentification();
```

With these converters, you can now use `ProductId` as an ID in your GraphQL schema. When you receive a `ProductId` ID in a request, HotChocolate will automatically use the `ProductId.Parse` method to convert it into a `ProductId` object. Likewise, when returning a `ProductId` object in a response, HotChocolate will use the `ToString` method to convert it back into a string.

# Global Object Identification

Global Object Identification, as the name suggests, is about being able to uniquely identify an object within our schema. Moreover, it allows consumers of our schema to refetch an object in a standardized way. This capability allows client applications, such as [Relay](https://relay.dev), to automatically refetch types.

To identify types that can be re-fetched, a new `Node` interface type is introduced.

```sdl
interface Node {
  id: ID!
}
```

Implementing this type signals to client applications, that the implementing type can be re-fetched. Implementing it also enforces the existence of an `id` field, a unique identifier, needed for the refetch operation.

To refetch the types implementing the `Node` interface, a new `node` field is added to the query.

```sdl
type Query {
  node(id: ID!): Node
}
```

While it is not part of the specification, it is recommended to add the ability for plural fetches. That's why Hot Chocolate adds a `nodes` field allowing us to refetch multiple objects in one round trip.

```sdl
type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

## Usage

In Hot Chocolate we can enable Global Object Identification, by calling `AddGlobalObjectIdentification()` on the `IRequestExecutorBuilder`.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification()
    .AddQueryType<Query>();
```

This registers the `Node` interface type and adds the `node(id: ID!): Node` and the `nodes(ids: [ID!]!): [Node]!` field to our query type. At least one type in our schema needs to implement the `Node` interface or an exception is raised.

> Warning: Using `AddGlobalObjectIdentification()` in two upstream stitched services does currently not work out of the box.

Next we need to extend our object types with the `Global Object Identification` functionality. Therefore 3 criteria need to be fulfilled:

1. The type needs to implement the `Node` interface.
2. On the type an `id` field needs to be present to properly implement the contract of the `Node` interface.
3. A method responsible for refetching an object based on its `id` needs to be defined.

<ExampleTabs>
<Implementation>

To declare an object type as a re-fetchable, we need to annotate it using the `[Node]` attribute. This in turn causes the type to implement the `Node` interface and if present automatically turns the `id` field into a [global identifier](#global-identifiers).

There also needs to be a method, a _node resolver_, responsible for the actual refetching of the object. Assuming our class is called `Product`, Hot Chocolate looks for a static method, with one of the following names:

- `Get`
- `GetAsync`
- `GetProduct`
- `GetProductAsync`

The method is expected to have a return type of either `Product` or `Task<Product>`. Furthermore the first argument of this method is expected to be of the same type as the `Id` property. At runtime Hot Chocolate will invoke this method with the `id` of the object that should be re-fetched. Special types, such as services, can be injected as arguments as well.

```csharp
[Node]
public class Product
{
    public string Id { get; set; }

    public static async Task<Product> Get(string id, ProductService service)
    {
        Product product = await service.GetByIdAsync(id);

        return product;
    }
}
```

If we need to influence the global identifier generation, we can annotate the `Id` property manually.

```csharp
[ID("Example")]
public string Id { get; set; }
```

If the `Id` property of our class is not called `id`, we can either [rename it](/docs/hotchocolate/v15/defining-a-schema/object-types#naming) or specify the name of the property that should be the `id` field through the `[Node]` attribute. Hot Chocolate will then automatically rename this property to `id` in the schema to properly implement the contract of the `Node` interface.

```csharp
[Node(IdField = nameof(ProductId))]
public class Product
{
    public string ProductId { get; set; }

    // Omitted code for brevity
}
```

If our _node resolver_ method doesn't follow the naming conventions laid out above, we can annotate it using the `[NodeResolver]` attribute to let Hot Chocolate know that this should be the method used for refetching the object.

```csharp
[NodeResolver]
public static Product OtherMethod(string id)
{
    // Omitted code for brevity
}
```

If we want to resolve the object using another class, we can reference the class/method like the following.

```csharp
[Node(NodeResolverType = typeof(ProductNodeResolver),
    NodeResolver = nameof(ProductNodeResolver.MethodName))]
public class Product
{
    public string ProductId { get; set; }
}

public class ProductNodeResolver
{
    public static Product MethodName(string id)
    {
        // Omitted code for brevity
    }
}
```

When placing the `Node` functionality in an extension type, it is important to keep in mind that the `[Node]` attribute needs to be defined on the class extending the original type.

```csharp
[Node]
[ExtendObjectType(typeof(Product))]
public class ProductExtensions
{
    public Product GetProductAsync(string id)
    {
        // Omitted code for brevity
    }
}
```

[Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

</Implementation>
<Code>

In the code-first approach, we have multiple APIs on the `IObjectTypeDescriptor` to fulfill these criteria:

- `ImplementsNode`: Implements the `Node` interface.
- `IdField`: Selects the property that represents the unique identifier of the object.
- `ResolveNode` / `ResolveNodeWith`: Method that re-fetches the object by its Id, also called the _node resolver_. If these methods are chained after `IdField`, they automatically infer the correct type of the `id` argument.

```csharp
public class Product
{
    public string Id { get; set; }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) =>
            {
                Product product =
                    await context.Service<ProductService>().GetByIdAsync(id);

                return product;
            });
    }
}
```

> Warning: When using middleware such as `UseDbContext` it needs to be chained after the `ResolveNode` call. The order of middleware still matters.

If the `Id` property of our class is not called `id`, we can either [rename it](/docs/hotchocolate/v15/defining-a-schema/object-types#naming) or specify it through the `IdField` method on the `IObjectTypeDescriptor`. Hot Chocolate will then automatically rename this property to `id` in the schema to properly implement the contract of the `Node` interface.

```csharp
public class Product
{
    public string ProductId { get; set; }
}

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(f => f.ProductId)
            .ResolveNode((context, id) =>
            {
                // Omitted code for brevity
            });
    }
}
```

In case we want to resolve the object using another class, we can do so using `ResolveNodeWith`.

```csharp
public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(f => f.ProductId)
            .ResolveNodeWith<ProductNodeResolver>(r =>
                r.GetProductAsync(default));
    }
}

public class ProductNodeResolver
{
    public async Task<Product> GetProductAsync(string id)
    {
        // Omitted code for brevity
    }
}
```

</Code>
<Schema>

The approach of either implementation-first or code-first can be used in conjunction with schema-first.

</Schema>
</ExampleTabs>

Since node resolvers resolve entities by their Id, they are the perfect place to start utilizing DataLoaders.

[Learn more about DataLoaders](/docs/hotchocolate/v15/fetching-data/dataloader)

# Connections

_Connections_ are a standardized way to expose pagination capabilities.

```sdl
type Query {
  users(first: Int after: String last: Int before: String): UsersConnection
}

type UsersConnection {
  pageInfo: PageInfo!
  edges: [UsersEdge!]
  nodes: [User!]
}

type UsersEdge {
  cursor: String!
  node: User!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}
```

[Learn more about Connections](/docs/hotchocolate/v15/fetching-data/pagination#connections)

# Query field in Mutation payloads

It's a common best practice to return a payload type from mutations containing the affected entity as a field.

```sdl
type Mutation {
  likePost(id: ID!): LikePostPayload
}

type LikePostPayload {
  post: Post
}
```

This allows us to immediately process the affected entity in the client application responsible for the mutation.

Sometimes a mutation might affect other parts of our application as well. Maybe the `likePost` mutation needs to update an Activity Feed.

For this scenario, we can expose a `query` field on our payload type to allow the client application to fetch everything it needs to update its state in one round trip.

```sdl
type LikePostPayload {
  post: Post
  query: Query
}
```

A resulting mutation request could look like the following.

```graphql
mutation {
  likePost(id: 1) {
    post {
      id
      content
      likes
    }
    query {
      ...ActivityFeed_Fragment
    }
  }
}
```

## Usage

Hot Chocolate allows us to automatically add this `query` field to all of our mutation payload types:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryFieldToMutationPayloads();
```

By default, this will add a field of type `Query` called `query` to each top-level mutation field type, whose name ends in `Payload`.

Of course these defaults can be tweaked:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
        options.MutationPayloadPredicate =
            (type) => type.Name.Value.EndsWith("Result");
    });
```

This would add a field of type `Query` with the name of `rootQuery` to each top-level mutation field type, whose name ends in `Result`.

> Warning: This feature currently doesn't work on a stitching gateway, however this will be addressed in a future release focused on stitching. It's tracked as [#3158](https://github.com/ChilliCream/graphql-platform/issues/3158).
