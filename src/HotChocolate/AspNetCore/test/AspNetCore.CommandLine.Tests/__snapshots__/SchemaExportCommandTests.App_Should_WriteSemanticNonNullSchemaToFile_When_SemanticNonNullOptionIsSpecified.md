# App_Should_WriteSemanticNonNullSchemaToFile_When_SemanticNonNullOptionIsSpecified

## Schema

```graphql
schema {
  query: Query
}

type Query {
  foo: String @semanticNonNull
}

directive @semanticNonNull(levels: [Int!] = [0]) on FIELD_DEFINITION

```

## Settings

```json
{
  "name": "_Default",
  "transports": {
    "http": {
      "url": "http://localhost:5000/graphql"
    }
  }
}

```
