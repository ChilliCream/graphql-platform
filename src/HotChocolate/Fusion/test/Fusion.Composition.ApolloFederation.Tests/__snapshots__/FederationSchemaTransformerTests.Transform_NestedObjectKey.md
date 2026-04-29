# Transform_NestedObjectKey

## Apollo Federation SDL

```graphql
schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
  query: Query
}

type Article @key(fields: "metadata { id }") {
  metadata: ArticleMetadata!
  title: String!
}

type ArticleMetadata {
  id: ID!
  author: String
}

type Query {
  article: Article
  _service: _Service!
  _entities(representations: [_Any!]!): [_Entity]!
}

type _Service { sdl: String! }

union _Entity = Article

scalar FieldSet
scalar _Any

directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
directive @link(url: String! import: [String!]) repeatable on SCHEMA
```

## Transformed SDL

```graphql
schema {
  query: Query
}

type Query {
  article: Article
  articleByMetadataAndId(
    key: ArticleByMetadataAndIdInput! @is(field: "{ metadata: metadata.{ id } }")
  ): Article @internal @lookup
}

type Article @key(fields: "metadata { id }") {
  metadata: ArticleMetadata!
  title: String!
}

type ArticleMetadata {
  author: String
  id: ID!
}

input ArticleByMetadataAndIdInput {
  metadata: ArticleByMetadataAndIdInput_Metadata
}

input ArticleByMetadataAndIdInput_Metadata {
  id: ID!
}
```
