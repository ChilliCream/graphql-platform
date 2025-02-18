# Same_Sub_Selection_Skipped_Twice

## Result

```json
{
  "data": {
    "product": {}
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product {
    brand @skip(if: $skip) {
      name
    }
    brand @skip(if: $skip) {
      id
    }
  }
}
```

## QueryPlan Hash

```text
2626677079720B1BC51B536214BDB7B2C1F348C4
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { brand @skip(if: $skip) { name } brand @skip(if: $skip) { id } } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { product { brand @skip(if: $skip) { name id } } }",
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

