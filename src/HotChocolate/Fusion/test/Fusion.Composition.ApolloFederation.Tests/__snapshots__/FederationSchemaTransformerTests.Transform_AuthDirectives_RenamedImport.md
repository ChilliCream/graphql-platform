# Transform_AuthDirectives_RenamedImport

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: [
      "@key"
      { name: "@authenticated", as: "@auth" }
      { name: "@requiresScopes", as: "@scopes" }
    ]
  ) {
  query: Query
}

type Product @key(fields: "id") @auth {
  id: ID!
  name: String @scopes(scopes: [["read:products"]])
}

type Query {
  product(id: ID!): Product
}

scalar FieldSet
scalar federation__Scope
scalar link__Import

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [link__Import]) repeatable on SCHEMA
directive @auth on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @scopes(scopes: [[federation__Scope!]!]!)
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
    @policy(names: [["fusion.authenticated"]], onDenied: ERROR)
    @policy(names: [["fusion.scope:read:products"]], onDenied: ERROR)
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
