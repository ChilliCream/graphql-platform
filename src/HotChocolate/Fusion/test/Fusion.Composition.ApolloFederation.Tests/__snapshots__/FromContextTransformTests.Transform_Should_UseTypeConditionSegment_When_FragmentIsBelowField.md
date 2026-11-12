# Transform_Should_UseTypeConditionSegment_When_FragmentIsBelowField

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
type Query {
  product: Product
  account: Account
}

type Account @context(name: "ctx") {
  id: ID!
  owner: Owner
}

interface Owner {
  id: ID!
}

type User implements Owner {
  id: ID!
  locale: String
}

type Product {
  id: ID!
  label(value: String @fromContext(field: "$ctx { owner { ... on User { locale } } }")): String
}
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  account: Account
  product: Product
}

type Account @context(name: "ctx") {
  id: ID!
  owner: Owner
}

type Product {
  id: ID!
  label(value: String @require(field: "$ctx.owner<User>.locale")): String
}

type User implements Owner {
  id: ID!
  locale: String
}

interface Owner {
  id: ID!
}
```
