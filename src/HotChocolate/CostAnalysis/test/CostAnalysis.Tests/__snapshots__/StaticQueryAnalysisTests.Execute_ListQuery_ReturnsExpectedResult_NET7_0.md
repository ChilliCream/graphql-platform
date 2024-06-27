# Execute_ListQuery_ReturnsExpectedResult

## Query

```graphql
{
  examples(limit: 10) {
    field1
    field2
  }
  __cost {
    requestCosts {
      fieldCounts {
        name
        value
      }
      typeCounts {
        name
        value
      }
      inputTypeCounts {
        name
        value
      }
      inputFieldCounts {
        name
        value
      }
      argumentCounts {
        name
        value
      }
      directiveCounts {
        name
        value
      }
    }
  }
}
```

## Result Result:

```text
{
  "errors": [
    {
      "message": "The field `__cost` does not exist on the type `Query`.",
      "locations": [
        {
          "line": 4,
          "column": 5
        }
      ],
      "extensions": {
        "type": "Query",
        "field": "__cost",
        "responseName": "__cost",
        "specifiedBy": "https://spec.graphql.org/October2021/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types"
      }
    }
  ]
}
```

## Schema

```text
type Query {
    examples(limit: Int): [Example!]! @listSize
}

type Example {
    field1: Boolean!
    field2: Int!
}
```

