# Same_Sub_Selection_Only_One_Skipped

## Result

```json
{
  "data": {
    "product": {
      "brand": null
    }
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
    brand {
      id
    }
  }
}
```

## QueryPlan Hash

```text
20B3F1E88D04B0D8A6C7C31B3FE4B6A21727E094
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product { brand @skip(if: $skip) { name } brand { id } } }",
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

