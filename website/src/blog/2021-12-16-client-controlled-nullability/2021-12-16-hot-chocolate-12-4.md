---
path: "/blog/2021/12/14/hot-chocolate-12-4"
date: "2021-12-16"
title: "Client Controlled Nullability"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "hot-chocolate-12-5-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Today we have released Hot Chocolate 12.5, and this release is packed with new features. We put a focus on adding some early spec proposals into this release. Further, we have completely overhauled the Banana Cake Pop IDE to include feedback from our community. Lastly, we picked up an issue created by Simon to support OpenTelemetry.

# Banana Cake Pop

Let us start with the most visible change to Hot Chocolate. With Hot Chocolate 12.5, we have integrated Banana Cake Pop iteration 22, which introduces themes support. One of the top requests for BCP by users was a Dark mode. With the new version, you can now switch between our light and our dark theme. We will add more themes with one of the subsequent iterations.

IMAGE

We put another focus on discoverability. Many people getting into BCP had difficulty finding the schema explorer or other details regarding their operation document. With the new version, the IDE is much more organized and exposes clearly areas you can dig into.

IMAGE

The new Banana Cake Pop version is now available online at https://eat.bananacakepop.com, as an application that you can download at https://bananacakepop.com or through the Hot Chocolate middleware.

# Open Telemetry

Hot Chocolate for a long time provides instrumentation events that can be used to add your logging solution. By doing this, we did not bind Hot Chocolate to a specific logging/tracing solution or a specific use-case.

But it also meant that almost everyone had to come up with their own solution to instrument Hot Chocolate. With Hot Chocolate 12.5, we have added the `HotChocolate.Diagnostics` package, which uses the new `ActivitySource` API.

To add OpenTelemetry to your GraphQL server, first add the activity instrumentation to your schema.

EXAMPLE

Next, we need to configure OpenTelemetry for our service. To quickly inspect our traces, we will use a Jaeger exported.

EXAMPLE

With all this in place, we can execute requests against our demo server and inspect the traces with the Jaeger UI.

IMAGE

The complete example can be found [here](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/OpenTelemetry).

# Oneof Input Objects

One of the most asked-for features in GraphQL is input unions. The GraphQL working group has been discussing this feature for a long time, and we have explored multiple roads to achieve this. The most likely candidate has become the _Oneof Input Object_ representing a structural union. A structural union means that _Oneof Input Object_ is a special kind of input object where each field represents one choice. The _Oneof Input Object_ will only allow one field to be set, and the value can not be null. The type system enforces the rules for _Oneof Input Objects_.

We support _Oneof Input Objects_ in all three schema-building approaches (annotation-based, code-first, and schema-first.

In order to make an input object a _Oneof Input Object_ you simply need to annotate it with the `@oneOf` directive.

EXAMPLE

Next, you need to enable the RFC feature on the schema.

EXAMPLE

The complete example can be found [here](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/OneOf).

Docs can be found [here](https://chillicream.com/docs/hotchocolate/defining-a-schema/input-object-types/#oneof-input-objects).

# Client-Side Nullability

Client-Side nullability gives more power to the consumer of a GraphQL API. It allows us to specify error boundaries in GraphQL by defining if a field shall be nullable or required in our GraphQL request.

EXAMPLE

# Schema Coordinates

# Old

Today we have released Hot Chocolate 12.5 and this release is packed with new features. We put an emphasis on adding some early spec proposals. These are experimental features that you can opt into and that will get more refined over time.

it the initial implementation of client-controlled nullability.

With Hot Chocolate 12.5, we have focused on a couple new spec drafts and on OpenTelemetry.

Today we have released Hot Chocolate 12.5.0-preview.1 and with it the initial implementation of client-controlled nullability.

This new RFC will introduce new query syntax to let the user override type nullability on fields and introduce error boundaries into GraphQL.

Let us for instance, say we have a schema like the following:

```graphql
type Query {
  me: User
}

type User {
  name: String!
  bio: String!
  friends: [User!]
}
```

In our little schema, we have a user object with a name, a bio, and friends. Lets now consider we have a simple query where we fetch the currently signed in user his or her friends like the following:

```graphql
{
  me {
    name
    bio
    friends {
      name
      bio
    }
  }
}
```

Let's say the `bio` field now comes from a second backend service, and whenever this fails, we do not mind, but it would be great to have it. In the current schema setup, the field `bio` is non-null, and whenever the GraphQL server has an error on this field, it would erase everything up to the `friends` field.

```json
{
  "me": {
    "name": "Michael Staib",
    "bio": "Author of Hot Chocolate ...",
    "friends": null
  }
}
```

With client-controlled nullability, the consumer of the API can change this by overriding the field type nullability.

```graphql
{
  me {
    name
    bio
    friends {
      name
      bio?
    }
  }
}
```

By adding a question mark, we can tell the execution engine that we do not mind if this field has null value.
So, adding a question mark allows will make our field nullable.

But we could also approach this differently and say if the field `bio` does not deliver any data, I do not want to have any data.

```graphql
{
  me! {
    name
    bio
    friends! {
      name
      bio
    }
  }
}
```

So, in this case, I added the bang operator to the field `me` and the field `friends`. In GraphQL, a non-null violation will bubble up until it reaches a nullable field or until the complete result is deleted. Since we made the root non-null, the whole result is deleted. Meaning either I get all the data I demanded or nothing.

We also could produce null entries in our friends list for users that did not have a value for `bio` with the new list nullability modifier `[?]`.

```graphql
{
  me! {
    name
    bio
    friends[?] {
      name
      bio
    }
  }
}
```

To take advantage of this new feature, you only need to update to Hot Chocolate 12.5.0-preview.1, and it will be available. We will polish the implementation further, and you can help us with that by providing feedback. At the moment, Banana Cake Pop is not updated for the new syntax. We will do that in the coming days. But you can write and execute the new syntax.

The current GraphQL spec RFC can be found [here](https://github.com/graphql/graphql-spec/pull/895/files).

If you want to try it out, you also can use our Hot Chocolate Workshop instance running [here](https://workshop.chillicream.com/graphql). We added a simple error argument that lets you simulate errors on the `bio` field of the speaker object. But also, without throwing an error, you can rewrite nullability and define your data type expectation.

We hope to have 12.5 out by the end of this year. We are also working on the **OneOf**, which should bring input unions for this release.
