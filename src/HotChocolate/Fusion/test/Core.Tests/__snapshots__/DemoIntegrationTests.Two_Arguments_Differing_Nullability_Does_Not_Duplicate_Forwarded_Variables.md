# Two_Arguments_Differing_Nullability_Does_Not_Duplicate_Forwarded_Variables

## Result

```json
{
  "data": {
    "testWithTwoArgumentsDifferingNullability": 2
  }
}
```

## Request

```graphql
query Test($number: Int!) {
  testWithTwoArgumentsDifferingNullability(first: $number, second: $number)
}
```

## QueryPlan Hash

```text
FF4DCF0AAD873BD22B2F69452CFF5036466A422C
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

