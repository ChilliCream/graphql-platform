# Same_TopLevel_Selection_Only_One_Skipped

## Result

```json
{
  "data": {
    "product": null
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  product @skip(if: $skip) {
    price
  }
  product {
    brand {
      name
    }
  }
}
```

## QueryPlan Hash

```text
57D85C7B4A843EC7C561D336A7A367DFA4E0C337
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { product @skip(if: $skip) { price } product { brand { name } } }",
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

