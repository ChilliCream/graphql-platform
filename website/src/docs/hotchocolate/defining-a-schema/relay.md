---
title: "Relay"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

TODO

[Learn more about the Relay GraphQL Server Specification](https://relay.dev/docs/guides/graphql-server-specification)

# Global identifiers

If an output type contains an `id: ID!` field, [Relay](https://relay.dev) and other GraphQL clients will consider this the unique identifier of the entity and might use it to construct a flat cache. This can be problematic, since we could have the same identifier for two of our types. When using SQL for example, a `Foo` and `Bar` type could both contain a row with the identifier `1` in their respective tables.

We could switch to a database technology that uses unique identifiers across tables/collections, but as soon as we introduce a different data source, we might be facing the same problem again.

Fortunately there is an easier, more integrated way to go about solving this problem in Hot Chocolate: Global identifiers.

TODO: maybe explain the feature is a middleware here already and how it doesnt affect business layer logic, types dont matter, etc.

## Usage in Output Types

When returning an Id in an output type, Hot Chocolate can automatically combine its value with another value to form a unique Id. Per default this additional value is the name of the type the Id belongs to. Since type names are unique within a schema, this ensures that we are returning a unique Id within the schema. If our GraphQL server serves multiple schemas, the schema name is also included in this combined Id. The resulting Id is then Base64 encoded to make it opaque.

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

</ExampleTabs.Schema>
</ExampleTabs>

## Id Serializer

Unique (or global) Ids are generated using the `IIdSerializer`. We can access it like any other service and use it to serialize or deserialize global Ids ourselves.

```csharp
public class Query
{
    public string Example([Service] IIdSerializer serializer)
    {
        string serializedId = serializer.Serialize(null, "User", "123");

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

This registers the `Node` interface type and adds the `node(id: ID!): Node` field to our query type, as explained above.

> ⚠️ Note: Using `AddGlobalObjectIdentification()` in two upstream stitched services does currently not work out of the box.

<ExampleTabs>
<ExampleTabs.Annotation>

TODO

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class User
{
    public string Id { get; set; }

    public string Name { get; set; }
}

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .ImplementsNode()
            .IdField(f => f.Id)
            .ResolveNode(async (context, id) =>
            {
                User user =
                    await context.Service<UserService>().GetByIdAsync(id);

                return user;
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

</ExampleTabs.Schema>
</ExampleTabs>

<!-- todo: how to resolve in different type / other options of GOI -->

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

This allows us to immediately use the affected entity in the client application responsible for the mutation.

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
