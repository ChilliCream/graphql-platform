With Hot Chocolate 12.5, we focus on new spec drafts, which we bring into the core as experimental features. Today we have released Hot Chocolate 12.5.0-preview.1 and with it the initial implementation of client-controlled nullability.

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
