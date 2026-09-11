# Transform_PolicyDirective_RenamedImport

## Apollo Federation SDL

```graphql
schema
  @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])
  @link(
    url: "https://specs.apollo.dev/policy/v0.1"
    import: [{ name: "@policy", as: "@authz" }]
  ) {
  query: Query
}

type Product @key(fields: "id") {
  id: ID!
  name: String @authz(policies: [["internal"]])
}

type Query {
  product(id: ID!): Product
}

scalar FieldSet
scalar federation__Policy
scalar link__Import

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [link__Import]) repeatable on SCHEMA
directive @authz(policies: [[federation__Policy!]!]!) repeatable
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

type Product @key(fields: "id") {
  id: ID!
  name: String @policy(names: [["internal"]])
}

enum PolicyDenialBehavior {
  NULL
  ERROR
  ABORT
}

scalar link__Import

directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior) repeatable on
  | OBJECT
  | FIELD_DEFINITION
```
