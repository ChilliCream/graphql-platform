# Execute_ListQuery_ReturnsExpectedResult

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

## Query

```text
query {
    examples { field1, field2 }

    __cost {
        requestCosts {
            fieldCounts { name, value }
            typeCounts { name, value }
            inputTypeCounts { name, value }
            inputFieldCounts { name, value }
            argumentCounts { name, value }
            directiveCounts { name, value }
        }
    }
}
```

## Result

```text
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
}
```

