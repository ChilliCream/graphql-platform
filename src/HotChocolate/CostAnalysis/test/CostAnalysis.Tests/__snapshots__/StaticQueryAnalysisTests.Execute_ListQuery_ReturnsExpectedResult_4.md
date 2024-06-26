# Execute_ListQuery_ReturnsExpectedResult

## Query

```graphql
{
  examples {
    field1
    field2
  }
}
```

## Result

```text
{
  "data": {
    "examples": [
      {
        "field1": true,
        "field2": 1
      }
    ]
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 1,
      "typeCost": 2
    }
  }
}
```

## Schema

```text
type Query {
    examples(limit: Int): [Example!]! @listSize(slicingArguments: ["limit"])
}

type Example {
    field1: Boolean!
    field2: Int!
}
```

