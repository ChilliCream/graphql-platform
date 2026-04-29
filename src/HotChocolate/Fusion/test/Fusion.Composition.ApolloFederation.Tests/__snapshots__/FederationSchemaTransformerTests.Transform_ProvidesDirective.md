# Transform_ProvidesDirective

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@provides"]) {
  query: Query
}

type User @key(fields: "id") {
  id: ID!
  username: String
  totalProductsCreated: Int
}

type Review {
  body: String
  author: User @provides(fields: "username")
}

type Query {
  reviews: [Review]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = User

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @provides(fields: FieldSet!) on FIELD_DEFINITION
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  reviews: [Review]
  userById(id: ID!): User @internal @lookup
}

type Review {
  author: User @provides(fields: "username")
  body: String
}

type User @key(fields: "id") {
  id: ID!
  totalProductsCreated: Int
  username: String
}
```
