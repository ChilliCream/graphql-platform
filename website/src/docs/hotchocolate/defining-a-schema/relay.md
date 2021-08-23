---
title: "Relay"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> Note: Even though they originated in Relay, the design principles described in this document are not exclusive to Relay. They lead to an overall better schema design, which is why we recommend them to **all** users of Hot Chocolate.

[Relay](https://relay.dev) is a JavaScript framework for building data-driven React applications with GraphQL, which is developed and used by _Facebook_.

As part of a specification Relay proposes some schema design principles for GraphQL servers in order to more efficiently fetch, refetch and cache entities on the client. In order to get the most performance out of Relay our GraphQL server needs to abide by these principles.

[Learn more about the Relay GraphQL Server Specification](https://relay.dev/docs/guides/graphql-server-specification)

# Global identifiers

If an output type contains an `id: ID!` field, [Relay](https://relay.dev) and other GraphQL clients will consider this the unique identifier of the entity and might use it to construct a flat cache. This can be problematic, since we could have the same identifier for two of our types. When using SQL for example, a `Foo` and `Bar` type could both contain a row with the identifier `1` in their respective tables.

We could switch to a database technology that uses unique identifiers across tables/collections, but as soon as we introduce a different data source, we might be facing the same problem again.

Fortunately there is an easier, more integrated way to go about solving this problem in Hot Chocolate: Global identifiers.

With Global Identifiers, Hot Chocolate adds a middleware that automatically serializes our identifiers to be unique within the schema. The concern of globally unique identifiers is therefore kept separate from our business domain and we can continue using the "real" identifiers within our business code, without worrying about uniqueness for a client.

## Usage in Output Types

When returning an Id in an output type, Hot Chocolate can automatically combine its value with another value to form a Global Id. Per default this additional value is the name of the type the Id belongs to. Since type names are unique within a schema, this ensures that we are returning a unique Id within the schema. If our GraphQL server serves multiple schemas, the schema name is also included in this combined Id. The resulting Id is then Base64 encoded to make it opaque.

We can opt Ids into this behavior like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Product
{
    [ID]
    public int Id { get; set; }
}
```

If no arguments are passed to the `[ID]` attribute, it will use the name of the output type, in this case `Product`, to serialize the Id.

If we do not want to use the name of the output type, we can specify a string of our choice.

```csharp
[ID("Foo")]
public int Id { get; set; }
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

If we do not want to use the name of the output type, we can specify a string of our choice.

```csharp
descriptor.Field(f => f.Id).ID("Foo");
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

The approach of either Annotation-based or Code-first can be used in conjunction with Schema-first.

</ExampleTabs.Schema>
</ExampleTabs>

The type of fields specified as `ID` is also automatically switched to the ID scalar.

[Learn more about the ID scalar](/docs/hotchocolate/defining-a-schema/scalars#id)

## Usage in Input Types

If our `Product` output type returns a serialized Id, all arguments and fields on input object types, accepting a `Product` Id, need to be able to interpret the serialized Id.
Therefore we also need to define them as `ID`, in order to deserialize the serialized Id to the actual Id.

<ExampleTabs>
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

The approach of either Annotation-based or Code-first can be used in conjunction with Schema-first.

</ExampleTabs.Schema>
</ExampleTabs>

## Id Serializer

Unique (or global) Ids are generated using the `IIdSerializer`. We can access it like any other service and use it to serialize or deserialize global Ids ourselves.

```csharp
public class Query
{
    public string Example([Service] IIdSerializer serializer)
    {
        string serializedId = serializer.Serialize(null, "Product", "123");

        IdValue deserializedIdValue = serializer.Deserialize(serializedId);
        object deserializedId = deserializedIdValue.Value;

        // Omitted code for brevity
    }
}
```

The `Serialize()` method takes the schema name as a first argument, followed by the type name and lastly the actual Id.

[Learn more about accessing services](/docs/hotchocolate/fetching-data/resolvers#injecting-services)

# Global Object Identification

Global Object Identification, as the name suggests, is about being able to uniquely identify an object within our schema. Moreover, it allows consumers of our schema to refetch an object in a standardized way. This capability allows client applications, such as [Relay](https://relay.dev), to automatically refetch types.

To identify types that can be refetched, a new `Node` interface type is introduced.

```sdl
interface Node {
  id: ID!
}
```

Implementing this type signals to client applications, that the implementing type can be refetched. Implementing it also enforces the existence of an `id` field, a unique identifier, needed for the refetch operation.

To refetch the types implementing the `Node` interface, a new `node` field is added to the query.

```sdl
type Query {
  node(id: ID!): Node
}
```

While it is not part of the specification, Hot Chocolate also adds a `nodes` field allowing you to refetch multiple objects in one round trip.

```sdl
type Query {
  node(id: ID!): Node
  nodes(ids: [ID!]!): [Node]!
}
```

## Usage

In Hot Chocolate we can enable Global Object Identification, by calling `AddGlobalObjectIdentification()` on the `IRequestExecutorBuilder`.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddGlobalObjectIdentification()
            .AddQueryType<Query>();
    }
}
```

This registers the `Node` interface type and adds the `node(id: ID!): Node` and the `nodes(ids: [ID!]!): [Node]!` field to our query type. At least one type in our schema needs to implement the `Node` interface or an exception is raised.

> ⚠️ Note: Using `AddGlobalObjectIdentification()` in two upstream stitched services does currently not work out of the box.

Next we need to extend our object types with the `Global Object Identification` functionality. Therefore 3 criteria need to be fulfilled:

1. The type needs to implement the `Node` interface.
2. On the type an `id` field needs to be present to properly implement the contract of the `Node` interface.
3. A method responsible for refetching an object based on its `id` needs to be defined.

<ExampleTabs>
<ExampleTabs.Annotation>

To declare an object type as a refetchable, we need to annotate it using the `[Node]` attribute. This in turn causes the type to implement the `Node` interface and if present automatically turns the `id` field into a [global identifier](#global-identifiers).

There also needs to be a method, a _node resolver_, responsible for the acutal refetching of the object. Assuming our class is called `Product`, Hot Chocolate looks for a static method, with one of the following names:

- `Get`
- `GetAsync`
- `GetProduct`
- `GetProductAsync`

The method is expected to have a return type of either `Product` or `Task<Product>`. Furthermore the first argument of this method is expected to be of the same type as the `Id` property. At runtime Hot Chocolate will invoke this method with the `id` of the object that should be refetched. Special types, such as services, can be injected as arguments as well.

```csharp
[Node]
public class Product
{
    public string Id { get; set; }

    public static async Task<Product> Get(string id,
        [Service] ProductService service)
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

If the `Id` property of our class is not called `id`, we can either [rename it](/docs/hotchocolate/defining-a-schema/object-types#naming) or specify the name of the property that should be the `id` field through the `[Node]` attribute. Hot Chocolate will then automatically rename this property to `id` in the schema to properly implement the contract of the `Node` interface.

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

In case we want to resolve the object using another class, we can reference the class / method like the following.

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

When wanting to place the `Node` functionality in an extension type, it is important to keep in mind that the `[Node]` attribute needs to be defined on the class extending the original type.

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

[Learn more about extending types](/docs/hotchocolate/defining-a-schema/extending-types)

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we have multiple APIs on the `IObjectTypeDescriptor` to fulfill these criteria:

- `ImplementsNode`: Implements the `Node` interface.
- `IdField`: Selects the property that represents the unique identifier of the object.
- `ResolveNode` / `ResolveNodeWith`: Method that refetches the object by its Id, also called the _node resolver_. If these methods are chained after `IdField`, they automatically infer the correct type of the `id` argument.

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

If the `Id` property of our class is not called `id`, we can either [rename it](/docs/hotchocolate/defining-a-schema/object-types#naming) or specify it through the `IdField` method on the `IObjectTypeDescriptor`. Hot Chocolate will then automatically rename this property to `id` in the schema to properly implement the contract of the `Node` interface.

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

The approach of either Annotation-based or Code-first can be used in conjunction with Schema-first.

</ExampleTabs.Schema>
</ExampleTabs>

Since node resolvers resolve entities by their Id, they are the perfect place to start utilizing DataLoaders.

[Learn more about DataLoaders](/docs/hotchocolate/fetching-data/dataloader)

# Connections

_Connections_ are a standardized way to expose pagination capabilities.

```sdl
type Query {
  users(first: Int after: String last: Int before: String): UserConnection
}

type UserConnection {
  pageInfo: PageInfo!
  edges: [UserEdge!]
  nodes: [User!]
}

type UserEdge {
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

[Learn more about Connections](/docs/hotchocolate/fetching-data/pagination#connections)

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

Sometimes a mutation might also affect other parts of our application as well. Maybe the `likePost` mutation needs to update an Activity Feed.

For this scenario we can expose a `query` field on our payload type to allow the client application to fetch everything it needs to update its state in one round trip.

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
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryFieldToMutationPayloads();
    }
}
```

By default, this will add a field of type `Query` called `query` to each top-level mutation field type, whose name ends in `Payload`.

Of course these defaults can be tweaked:

```csharp
services
    .AddGraphQLServer()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
        options.MutationPayloadPredicate =
            (type) => type.Name.Value.EndsWith("Result");
    });
```

This would add a field of type `Query` with the name of `rootQuery` to each top-level mutation field type, whose name ends in `Result`.
