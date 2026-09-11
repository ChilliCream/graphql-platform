# Transform_PolicyDirective_OnObjectType

## Apollo Federation SDL

```graphql
schema
  @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
  @link(url: "https://specs.apollo.dev/policy/v0.1", import: ["@policy"]) {
  query: Query
}

type Product @key(fields: "id") @policy(policies: [["internal"], ["support"]]) {
  id: ID!
  name: String
}

type Query {
  product(id: ID!): Product
}

scalar FieldSet
scalar federation__Policy

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
directive @policy(policies: [[federation__Policy!]!]!) repeatable
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

type Product @key(fields: "id") @policy(names: [["internal"], ["support"]]) {
  id: ID!
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
