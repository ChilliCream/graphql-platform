# App_Should_WriteSchemaToFile_When_OutputOptionIsSpecified

## Schema

```graphql
schema {
  query: Query
}

type Query {
  foo: String
}
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

