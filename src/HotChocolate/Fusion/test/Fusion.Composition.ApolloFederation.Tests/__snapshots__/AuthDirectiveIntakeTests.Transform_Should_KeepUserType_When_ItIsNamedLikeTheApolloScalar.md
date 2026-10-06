# Transform_Should_KeepUserType_When_ItIsNamedLikeTheApolloScalar

## Apollo Federation SDL

```graphql
schema
  @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@requiresScopes"]) {
  query: Query
}

type Query {
  policy: Policy @requiresScopes(scopes: [["read"]])
}

type Policy {
  id: ID!
}

directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  policy: Policy @requiresScopes(scopes: [["read"]])
}

type Policy {
  id: ID!
}
```
