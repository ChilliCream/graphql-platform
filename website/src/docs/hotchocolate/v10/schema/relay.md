---
title: Relay
---

[Relay](https://facebook.github.io/relay) is a _JavaScript_ framework for building data-driven React applications with GraphQL which is developed and used by _Facebook_.

Relay makes three assumptions about the backend which you have to abide by in order that your GraphQL backend plays well with this framework.

We recommend that you abide to the relay server specifications even if you do not plan to use relay since even _Apollo_ supports these specifications and they are really good guidelines that lead to a better schema design.

# Object Identification

The first specification is called [Relay Global Object Identification Specification](https://facebook.github.io/relay/graphql/objectidentification.htm) and defines that object identifiers are specified in a standardized way. Moreover, it defines that all identifier is schema unique and that we can refetch any object just by providing that identifier.

In order to support the schema has to provide an interface `Node` that looks like following:

```sdl
interface Node {
  id: ID!
}
```

Each object that exposes an identifier has to implement `Node` and provide the `id` field.

Moreover, the `Query` type has to expose a field `node` that can return a node for an id.

```sdl
type Query {
  ...
  node(id: ID!) : Node
  ...
}
```

This allows now the client APIs to automatically refetch objects from the server if the client framework wants to update its caches or if it has part of the object in its store and wants to fetch additional fields of an object.

Hot Chocolate makes implementing this very easy. First, we have to declare on our schema that we want to be relay compliant:

```csharp
ISchema schema = SchemaBuilder.New()
    .EnableRelaySupport()
    ...
    .Create();
```

This basically sets up a middleware to encode out identifiers to be schema unique, so you do not have to provide schema unique identifiers. Moreover, it will add a `Node` interface type and configure the `node` field on our query type.

Lastly, we have to declare on our object types that they are nodes and how they can be resolved.

```csharp
public class MyObjectType
    : ObjectType<MyObject>
{
    protected override void Configure(IObjectTypeDescriptor<MyObject> descriptor)
    {
        descriptor.AsNode()
            .IdField(t => t.Id)
            .NodeResolver((ctx, id) =>
                ctx.Service<IMyRepository>().GetMyObjectAsync(id));
        ...
    }
}
```

On the descriptor we mark the object as a node with `AsNode` after that we specify the property that represents our internal identifier, last but not least we specify the node resolver that will fetch the node from the database when it is requested through the `node` field on the query type.

There are more variants possible and you can even write custom resolvers and do not have to bind to an explicit property.

# Connections

The pagination specification is called [Relay Cursor Connections Specification](https://facebook.github.io/relay/graphql/connections.htm) and contains functionality to make manipulating one-to-many relationships easy, using a standardized way of expressing these one-to-many relationships. This standard connection model offers ways of slicing and paginating through the connection.

The relay style pagination is really powerful and with Hot Chocolate it is quite simple to implement.

If your database provider can provide it\`s data through `IQueryable` then implementing relay pagination is one line of code:

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Strings).UsePaging<StringType>();
    }
}
```

**We have a lot more documentation on pagination [here](/docs/hotchocolate/v10/data-fetching/pagination).**

# Mutations

The last specification is called [Relay Input Object Mutations Specification](https://facebook.github.io/relay/graphql/mutations.htm) and it describes how mutations should be specified. This is more a design guideline then something we could help you with APIs with.

Nevertheless, with version 9.1 we will try aide this with some convenience:
[Automatic Relay InputType](https://github.com/ChilliCream/hotchocolate/issues/773).

# Additional Information

The relay server specifications are also summarized and explained [here](https://facebook.github.io/relay/docs/en/graphql-server-specification). Also, if you have further questions head over to our slack channel.
