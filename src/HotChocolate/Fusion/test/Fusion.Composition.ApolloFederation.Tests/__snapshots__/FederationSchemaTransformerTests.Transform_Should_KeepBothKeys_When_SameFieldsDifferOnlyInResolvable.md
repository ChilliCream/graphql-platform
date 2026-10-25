# Transform_Should_KeepBothKeys_When_SameFieldsDifferOnlyInResolvable

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Item @key(fields: "a b", resolvable: false) @key(fields: "b a") {
  a: ID!
  b: String!
  name: String
}

type Query {
  items: [Item]
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = Item

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_itemByBAndA(a: ID!, b: String!): Item @internal @lookup
  items: [Item]
}

type Item @key(fields: "a b") @key(fields: "b a") {
  a: ID!
  b: String!
  name: String
}
```
