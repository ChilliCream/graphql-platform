# Transform_Should_PreserveOnlyConditionedUnionProvidesFields

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_bookById(id: ID!): Book @internal @lookup
  fusion__lookup_movieById(id: ID!): Movie @internal @lookup
  media: [Media] @shareable @provides(fields: "... on Book { title }")
  wrapper: Wrapper @provides(fields: "media { ... on Book { subtitle } }")
}

type Book @key(fields: "id") {
  id: ID!
  subtitle: String @external
  title: String @external
}

type Movie @key(fields: "id") {
  id: ID!
}

type Wrapper {
  media: Media
}

union Media = Book | Movie
```
