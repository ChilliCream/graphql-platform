# Transform_Should_KeepNameTypeAndDefaultValue_When_ArgumentHasFromContext

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
  user: User
}

type User @context(name: "userContext") {
  id: ID!
  locale: String
}

type Product {
  id: ID!
  label(locale: String! = "en" @fromContext(field: "$userContext { locale }")): String
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
  label(locale: String! = "en" @require(field: "$userContext.locale")): String
}

type User @context(name: "userContext") {
  id: ID!
  locale: String
}
```
