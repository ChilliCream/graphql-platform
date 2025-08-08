# App_Should_WriteSchemaToFile_When_OutputOptionIsSpecified

## Console Output

```text
Exported Files:
- /var/folders/gq/m81fj9xn2191pkjsw1hxrq6w0000gn/T/tmpG8XqS5.tmp.graphqls
- /var/folders/gq/m81fj9xn2191pkjsw1hxrq6w0000gn/T/tmpG8XqS5.tmp-settings.json

```

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

