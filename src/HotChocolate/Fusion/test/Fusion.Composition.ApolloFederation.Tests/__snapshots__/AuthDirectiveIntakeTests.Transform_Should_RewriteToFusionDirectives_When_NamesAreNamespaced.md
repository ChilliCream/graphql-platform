# Transform_Should_RewriteToFusionDirectives_When_NamesAreNamespaced

## Apollo Federation SDL

```graphql
schema
  @link(url: "https://specs.apollo.dev/federation/v2.6", as: "fed") {
  query: Query
}

type Query {
  a: String @fed__authenticated
  b: String @fed__requiresScopes(scopes: [["read"]])
  c: String @fed__policy(policies: [["admin"]])
}

scalar fed__Scope
scalar fed__Policy

directive @fed__authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @fed__requiresScopes(scopes: [[fed__Scope!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @fed__policy(policies: [[fed__Policy!]!]!)
  on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
directive @link(url: String! import: [String!] as: String) repeatable on SCHEMA
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
```
