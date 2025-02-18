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
55E292C2915D1077D46B29BB67F7D10ECB5292FF
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
        "document": "query Test_1($skip: Boolean!) { product @skip(if: $skip) { price brand { name } } }",
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

