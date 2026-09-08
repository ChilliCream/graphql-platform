# Transform_Should_TranslateAuthenticated_When_UnlinkedPolicyDefinitionPresent

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: ["@key", "@authenticated"]
  ) {
  query: Query
}

type Product @key(fields: "id") @authenticated {
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
directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
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

type Product
  @key(fields: "id")
  @policy(names: [["fusion.authenticated"]], onDenied: ERROR) {
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
