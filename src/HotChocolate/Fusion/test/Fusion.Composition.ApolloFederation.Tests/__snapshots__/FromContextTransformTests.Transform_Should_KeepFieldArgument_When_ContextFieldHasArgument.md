# Transform_Should_KeepFieldArgument_When_ContextFieldHasArgument

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@external", "@context", "@fromContext"]) {
  query: Query
}

scalar FieldSet
scalar ContextFieldValue

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @external on FIELD_DEFINITION | OBJECT
directive @context(name: String!) repeatable on INTERFACE | OBJECT | UNION
directive @fromContext(field: ContextFieldValue) on ARGUMENT_DEFINITION
directive @link(url: String! import: [String!]) repeatable on SCHEMA
enum Unit {
  METRIC
  IMPERIAL
}

type Query {
  product: Product
  user: User
}

type User @context(name: "ctx") {
  id: ID!
  prop(unit: Unit): String
}

type Product {
  id: ID!
  label(value: String @fromContext(field: "$ctx { prop(unit: METRIC) }")): String
}
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  product: Product
  user: User
}

type Product {
  id: ID!
  label(value: String @require(field: "$ctx.prop(unit: METRIC)")): String
}

type User @context(name: "ctx") {
  id: ID!
  prop(unit: Unit): String
}

enum Unit {
  METRIC
  IMPERIAL
}
```
