---
title: "Relay"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

TODO

[Learn more about the Relay GraphQL Server Specification](https://relay.dev/docs/guides/graphql-server-specification)

## Global identifiers

If an output type contains an `id: ID!` field, [Relay](https://relay.dev) and other GraphQL clients will consider this the unique identifier of the entity and might use it to construct a flat cache. This can be problematic, since we could have the same identifier for two of our types. When using SQL for example, a `Foo` and `Bar` type could both contain a row with the identifier `1` in their respective tables.

We could switch to a database technology that uses unique identifiers across tables/collections, but as soon as we introduce a different data source, we might be facing the same problem again.

Fortunately there is an easier, more integrated way to go about solving this problem in Hot Chocolate. The name of the type as well as the actual Id can automatically be combined to create an identifier that's unique within the schema.

TODO: switch in output and input type

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach, we just need to annotate fields and arguments using the `[GlobalId]` attribute.

```csharp
public class Product
{
    [GlobalId]
    public int Id { get; set; }
}

public class Query
{
    public Product GetProduct([GlobalId] int id)
        => new() { Id = id };
}
```

If no arguments are passed to the `[GlobalId]` attribute on a field, it will use the schema name of the type to produce the unique identifier.

We can also specify a custom string that should be used to produce the unique identifier.

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
        descriptor.Field(f => f.Id).GlobalId();
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Argument("id", a => a.Type<NonNullType<IdType>>().GlobalId())
            .Type<ProductType>()
            .Resolve(context =>
            {
                var id = context.ArgumentValue<int>("id");

                return new Product { Id = id };
            });
    }
}
```

> Note: `GlobalId()` can only be used on fields and arguments with a concrete type. Otherwise type modifiers like non-null or list can not be correctly rewritten.

If no arguments are passed to `GlobalId()`, it will use the schema name of the type to produce the unique identifier.

We can also specify a custom string that should be used to produce the unique identifier.

```csharp
descriptor.Field(f => f.Id).GlobalId("Foo");
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>

Fields and arguments defined like above are still represented using the `ID` scalar, but now they are Base64 encoded and contain the name of the type as well as the actual Id.

Similar to the regular coercion of the `ID` scalar, the Id values are encoded when used in an output type, and decoded when used in an input type.

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

<!-- todo: code to automatically open all external links in a new tab: <a target="_blank" rel="noopener noreferrer" href="link">...</a> -->

[Learn more about Global Object Identification](https://graphql.org/learn/global-object-identification)

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

> ⚠️ Note: Using `AddGlobalObjectIdentification()` in two stitched services does currently not work.

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

TODO

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
