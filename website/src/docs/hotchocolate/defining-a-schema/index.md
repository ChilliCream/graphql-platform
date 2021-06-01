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

Nested (object) types can also be arguments in the form of [input object types](/docs/hotchocolate/defining-a-schema/input-object-types). These are most commonly used when passing a payload to a mutation.

## Lists and Non-Null

TODO

## Type extensions

TODO

## Directives

TODO

<!-- todo: not sure about this title -->

## Schema maintenance

In GraphQL we have a variety of tools to expose information about our API to consumers:

- We can add [documentation](/docs/hotchocolate/defining-a-schema/documentation) for almost everything in our schema.

- [Versioning](/docs/hotchocolate/defining-a-schema/versioning) in GraphQL works a little different than in REST APIs. We try to evolve our schema in a non-breaking manner, by deprecating existing fields.
