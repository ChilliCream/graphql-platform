# Execute_ListQuery_ReturnsExpectedResult

## Schema

```text
type Query {
    examples(limit: Int): [Example!]!
    @listSize(slicingArguments: ["limit"], requireOneSlicingArgument: false)
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
  "data": {
    "examples": [
      {
        "field1": true,
        "field2": 1
      }
    ],
    "__cost": {
      "requestCosts": {
        "fieldCounts": [
          {
            "name": "Query.examples",
            "value": 1
          },
          {
            "name": "Example.field1",
            "value": 1
          },
          {
            "name": "Example.field2",
            "value": 1
          }
        ],
        "typeCounts": [
          {
            "name": "Query",
            "value": 1
          },
          {
            "name": "Example",
            "value": 1
          },
          {
            "name": "Boolean",
            "value": 1
          },
          {
            "name": "Int",
            "value": 1
          }
        ],
        "inputTypeCounts": [],
        "inputFieldCounts": [],
        "argumentCounts": [],
        "directiveCounts": []
      }
    }
  }
}
```

