# Transform_Should_RewriteToFusionDirectives_When_DirectivesAreImportedByName

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: ["@key", "@authenticated", "@requiresScopes", "@policy"]
  ) {
  query: Query
}

type Query {
  product(id: ID!): Product @authenticated
  secret: String @requiresScopes(scopes: [["read", "write"], ["admin"]])
  audited: String @policy(policies: [["auditor"]])
  single: String @requiresScopes(scopes: "read")
  flat: String @policy(policies: ["a", "b"])
}

type Product @key(fields: "id") @authenticated {
  id: ID!
  name: String @requiresScopes(scopes: [["product:read"]])
}

interface Node @policy(policies: [["admin"]]) {
  id: ID! @authenticated
}

enum Role @authenticated {
  ADMIN
}

scalar Secret @requiresScopes(scopes: [["secret"]])

scalar federation__Scope
scalar federation__Policy
scalar FieldSet

directive @key(fields: FieldSet!) repeatable on OBJECT | INTERFACE
directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @requiresScopes(scopes: [[federation__Scope!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @policy(policies: [[federation__Policy!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  audited: String @policy(policies: [["auditor"]])
  flat: String @policy(policies: [["a"], ["b"]])
  fusion__lookup_productById(id: ID!): Product @internal @lookup
  product(id: ID!): Product @authenticated
  secret: String @requiresScopes(scopes: [["read", "write"], ["admin"]])
  single: String @requiresScopes(scopes: [["read"]])
}

type Product @key(fields: "id") @authenticated {
  id: ID!
  name: String @requiresScopes(scopes: [["product:read"]])
}

interface Node @policy(policies: [["admin"]]) {
  id: ID! @authenticated
}

enum Role @authenticated {
  ADMIN
}

scalar Secret @requiresScopes(scopes: [["secret"]])
```
