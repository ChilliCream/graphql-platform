---
title: "Overview"
---

In this section we will learn everything that is needed to build an expressive GraphQL schema.

# Operations

First we will look at the three root types, often called _Operations_, that represent entry points to our schema:

- Queries allow us to _query_ our graph and retrieve data in a readonly manner.<br />[Learn more about queries](/docs/hotchocolate/v14/defining-a-schema/queries)

- Mutations allow us to _mutate_ our graph entities in the form of adding, removing or updating entities.<br />[Learn more about mutations](/docs/hotchocolate/v14/defining-a-schema/mutations)

- Subscriptions allow us to _subscribe_ to events in our system and be notified in real-time of their occurrence.<br />[Learn more about subscriptions](/docs/hotchocolate/v14/defining-a-schema/subscriptions)

# Types

Each GraphQL schema is made up of two basic building blocks:

- Object types contain fields and describe our entities.<br />[Learn more about object types](/docs/hotchocolate/v14/defining-a-schema/object-types)

- Scalars are the primitives of our GraphQL schema: `String`, `Int`, etc.<br />We can also define custom scalars to more precisely describe our business domain.<br />[Learn more about scalars](/docs/hotchocolate/v14/defining-a-schema/scalars)

There are also more advanced types:

- Enums are a special kind of scalar, restricted to a particular set of allowed values.<br />[Learn more about enums](/docs/hotchocolate/v14/defining-a-schema/enums)
- Interfaces represent a shared contract that other types can implement.<br />[Learn more about interfaces](/docs/hotchocolate/v14/defining-a-schema/interfaces)
- Unions represent a set of object types, without the need for a shared contract.<br />[Learn more about unions](/docs/hotchocolate/v14/defining-a-schema/unions).

# Type Modifiers

Besides regular types, like scalars and object types, there are also _type modifiers_.

A non-null field for example indicates that a client can always expect a non-null value to be returned from the field.

[Learn more about non-null](/docs/hotchocolate/v14/defining-a-schema/non-null)

List fields indicate to a client that the field will return a list in the specified shape.

[Learn more about lists](/docs/hotchocolate/v14/defining-a-schema/lists)

# Arguments

We can pass arguments to individual fields on an object type and access their values inside the field's resolver.

[Learn more about arguments](/docs/hotchocolate/v14/defining-a-schema/arguments)

Nested object types can also be used as arguments by declaring so called input object types. These are most commonly used when passing a payload to a mutation.

[Learn more about input object types](/docs/hotchocolate/v14/defining-a-schema/input-object-types)

# Extending Types

Hot Chocolate allows us to extend existing types, helping us keep our code organized.

Rather than adding more and more fields to the Query type in the same class for instance, we can _extend_ the Query type with a new field from another location in our codebase where that field logically should live.

[Learn more about extending types](/docs/hotchocolate/v14/defining-a-schema/extending-types)

# Directives

Directives allow us to decorate parts of our GraphQL schema with additional configuration.

This configuration can be used as metadata for client tools or alternate our GraphQL server's runtime execution and type validation behavior.

[Learn more about directives](/docs/hotchocolate/v14/defining-a-schema/directives)

# Schema evolution

As our data graph and number of developers/clients grows, we need to ensure that the graph is understood by everyone. Therefore, our schema should expose as much information to consumers of our API as possible.

[Learn more about schema documentation](/docs/hotchocolate/v14/defining-a-schema/documentation)

[Learn more about versioning](/docs/hotchocolate/v14/defining-a-schema/versioning)

# Relay

[Relay](https://relay.dev) proposes some schema design principles for GraphQL servers in order to more efficiently fetch, refetch and cache entities on the client. Since these principles make for a better schema, we encourage all users, not only those of Relay, to consider these principles.

[Learn more about Relay-compatible schema design](/docs/hotchocolate/v14/defining-a-schema/relay)

# Automatic type registration

Starting with Hot Chocolate 12.7 we introduced a new source generator that automatically registers types and DataLoader with your GraphQL configuration builder. Watch on YouTube how you can simplify your Hot Chocolate configuration code.

<Video videoId="QPelWd9L9ck" />
