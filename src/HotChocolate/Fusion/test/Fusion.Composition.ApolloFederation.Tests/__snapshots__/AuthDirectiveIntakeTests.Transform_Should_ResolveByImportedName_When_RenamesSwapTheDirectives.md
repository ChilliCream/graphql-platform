# Transform_Should_ResolveByImportedName_When_RenamesSwapTheDirectives

## Apollo Federation SDL

```graphql
schema
  @link(
    url: "https://specs.apollo.dev/federation/v2.6"
    import: [
      { name: "@requiresScopes", as: "@policy" }
      { name: "@policy", as: "@requiresScopes" }
    ]
  ) {
  query: Query
}

type Query {
  a: String @policy(scopes: [["read"]])
  b: String @requiresScopes(policies: [["admin"]])
}

directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  a: String @requiresScopes(scopes: [["read"]])
  b: String @policy(policies: [["admin"]])
}
```
