# Same_TopLevel_Selection_Skipped_Twice

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product @skip(if: $skip) {
    price
  }
  product @skip(if: $skip) {
    brand {
      name
    }
  }
}
```

## QueryPlan Hash

```text
D6F2417F20DCBFB793319B5B51D05AA552AAF440
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product @skip(if: $skip) { price } product @skip(if: $skip) { brand { name } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { product @skip(if: $skip) @skip(if: $skip) { price brand { name } } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "skip"
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

