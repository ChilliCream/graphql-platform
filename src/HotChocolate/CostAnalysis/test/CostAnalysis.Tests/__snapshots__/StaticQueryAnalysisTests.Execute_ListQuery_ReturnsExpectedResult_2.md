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
    examples(limit: 10) { field1, field2 }

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
            "value": 10
          },
          {
            "name": "Example.field2",
            "value": 10
          }
        ],
        "typeCounts": [
          {
            "name": "Query",
            "value": 1
          },
          {
            "name": "Example",
            "value": 10
          },
          {
            "name": "Boolean",
            "value": 10
          },
          {
            "name": "Int",
            "value": 10
          }
        ],
        "inputTypeCounts": [],
        "inputFieldCounts": [],
        "argumentCounts": [
          {
            "name": "Query.examples(limit:)",
            "value": 1
          }
        ],
        "directiveCounts": []
      }
    }
  }
}
```

