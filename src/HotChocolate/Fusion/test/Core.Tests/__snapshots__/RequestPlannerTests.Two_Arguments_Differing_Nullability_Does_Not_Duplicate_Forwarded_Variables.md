# Two_Arguments_Differing_Nullability_Does_Not_Duplicate_Forwarded_Variables

## UserRequest

```graphql
query Test($number: Int!) {
  testWithTwoArgumentsDifferingNullability(first: $number, second: $number)
}
```

## QueryPlan

```json
{
  "document": "query Test($number: Int!) { testWithTwoArgumentsDifferingNullability(first: $number, second: $number) }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Accounts",
        "document": "query Test_1($number: Int!) { testWithTwoArgumentsDifferingNullability(first: $number, second: $number) }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "number"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

