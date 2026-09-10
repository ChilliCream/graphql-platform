# Transform_Should_GenerateSingleLookup_When_SameKeyIsRepeatedOnTypeAndExtension

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Item @key(fields: "id") {
  id: ID!
  name: String
}

extend type Item @key(fields: "id") {
  quantity: Int
}

type Query {
  item(id: ID!): Item
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
  fusion__lookup_itemById(id: ID!): Item @internal @lookup
  item(id: ID!): Item
}

type Item @key(fields: "id") {
  id: ID!
  name: String
  quantity: Int
}
```
