# EntityResolver_Skip_On_SubField

## Result

```json
{
  "data": {
    "productById": {
      "price": 123.456
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") {
    name @skip(if: $skip)
    price
  }
}
```

## QueryPlan Hash

```text
38DB93D5058D1661AB219912CD754C469DB36C49
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) { name @skip(if: $skip) price } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { productById(id: \u00221\u0022) { name @skip(if: $skip) price } }",
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

