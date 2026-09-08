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

## Result Result:

```text
{
  "errors": [
    {
      "message": "Exactly one slicing argument must be defined.",
      "locations": [
        {
          "line": 2,
          "column": 5
        }
      ],
      "path": [
        "examples"
      ],
      "extensions": {
        "code": "HC0082"
      }
    }
  ]
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
