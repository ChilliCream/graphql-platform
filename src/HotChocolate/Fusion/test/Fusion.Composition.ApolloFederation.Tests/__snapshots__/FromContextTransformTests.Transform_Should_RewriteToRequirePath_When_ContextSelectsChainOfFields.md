# Transform_Should_RewriteToRequirePath_When_ContextSelectsChainOfFields

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
  userCurrency: Currency!
}

type Currency {
  isoCode: String!
}

type Product {
  id: ID!
  price(currency: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float
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

type Currency {
  isoCode: String!
}

type Product {
  id: ID!
  price(currency: String @require(field: "$userContext.userCurrency.isoCode")): Float
}

type User @context(name: "userContext") {
  id: ID!
  userCurrency: Currency!
}
```
