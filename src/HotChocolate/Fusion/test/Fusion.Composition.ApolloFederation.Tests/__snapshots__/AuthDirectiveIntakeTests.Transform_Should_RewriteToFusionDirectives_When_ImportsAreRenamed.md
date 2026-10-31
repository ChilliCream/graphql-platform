# Transform_Should_RewriteToFusionDirectives_When_ImportsAreRenamed

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: [
      "@key"
      { name: "@authenticated", as: "@auth" }
      { name: "@requiresScopes", as: "@scopes" }
      { name: "@policy", as: "@apolloPolicy" }
      { name: "Scope", as: "AuthScope" }
      { name: "Policy", as: "AuthPolicy" }
    ]
  ) {
  query: Query
}

type Query {
  a: String @auth
  b: String @scopes(scopes: [["read"]])
  c: String @apolloPolicy(policies: [["admin"]])
}

interface Node @apolloPolicy(policies: [["admin"]]) {
  id: ID! @auth
}

scalar AuthScope
scalar AuthPolicy
scalar FieldSet

directive @key(fields: FieldSet!) repeatable on OBJECT | INTERFACE
directive @auth on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @scopes(scopes: [[AuthScope!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @apolloPolicy(policies: [[AuthPolicy!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  a: String @authenticated
  b: String @requiresScopes(scopes: [["read"]])
  c: String @policy(policies: [["admin"]])
}

interface Node @policy(policies: [["admin"]]) {
  id: ID! @authenticated
}
```
