# Transform_AuthenticatedDirective_OnField

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: ["@key", "@authenticated"]
  ) {
  query: Query
}

type Product @key(fields: "id") {
  id: ID!
  name: String
  internalNotes: String @authenticated
}

type Query {
  product(id: ID!): Product
}

scalar FieldSet

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  fusion__lookup_productById(id: ID!): Product @internal @lookup
  product(id: ID!): Product
}

type Product @key(fields: "id") {
  id: ID!
  internalNotes: String
    @policy(names: [["fusion.authenticated"]], onDenied: ERROR)
  name: String
}

enum PolicyDenialBehavior {
  NULL
  ERROR
  ABORT
}

directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior) repeatable on
  | OBJECT
  | FIELD_DEFINITION
```
