---
title: "Overview"
---

In this section we will learn everything that is needed to build an expressive GraphQL schema.

## Operations

First we will look at the three root types, often called _Operations_, that represent entry points to our schema:

- [Queries](/docs/hotchocolate/defining-a-schema/queries) allow us to _query_ our graph and retrieve data in a readonly manner.

- [Mutations](/docs/hotchocolate/defining-a-schema/mutations) allow us to _mutate_ our graph entities in the form of adding, removing or updating entities.

- [Subscriptions](/docs/hotchocolate/defining-a-schema/subscriptions) allow us to _subscribe_ to events in our system and be notified in real-time of their occurrence.

## Types

Each GraphQL schema is made up of two basic building blocks:

- [_Object types_](/docs/hotchocolate/defining-a-schema/object-types) contain fields and describe our entities.<br />[Operations](#Operations) for example are nothing more than simple object types.

- [Scalars](/docs/hotchocolate/defining-a-schema/scalars) are the primitives of our GraphQL schema: `String`, `Int`, etc.<br />We can also define custom scalars to more precisely describe our business domain.

Besides those there are also [enums](/docs/hotchocolate/defining-a-schema/enums), [interfaces](/docs/hotchocolate/defining-a-schema/interfaces) and [unions](/docs/hotchocolate/defining-a-schema/unions).

## Arguments

We can pass [arguments](/docs/hotchocolate/defining-a-schema/arguments) to individual fields on an object type and access their values inside the field's resolver.

Nested (object) types can also be used as arguments by declaring so called [input object types](/docs/hotchocolate/defining-a-schema/input-object-types). These are most commonly used when passing a payload to a mutation.

## Type Modifiers

Besides regular types, like scalars and object types, there are also _type modifiers_.

A [Non-Null](/docs/hotchocolate/defining-a-schema/non-null) field for example indicates that a client can always expect a non-null value to be returned from the field.

[List](/docs/hotchocolate/defining-a-schema/lists) fields indicate to a client that the field will return an list in the specified shape.

## Extending Types

Hot Chocolate allows us to [extend existing types](/docs/hotchocolate/defining-a-schema/extending-types), helping us keep our code organized.

## Directives

[Directives](/docs/hotchocolate/defining-a-schema/directives) allow us to decorate parts of our GraphQL schema with additional configuration.

This configuration can be used as metadata for client tools or alternate our GraphQL server's runtime execution and type validation behavior.

## Schema maintenance

We also have other means of exposing information about our API to consumers, besides [Directives](/docs/hotchocolate/defining-a-schema/directives):

- We can add [documentation](/docs/hotchocolate/defining-a-schema/documentation) for almost everything in our schema.

- Fields can be [deprecated](/docs/hotchocolate/defining-a-schema/versioning), signaling to consumers that a field should no longer be used.
