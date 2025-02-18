# Same_TopLevel_Selection_Only_One_Skipped

## Result

```json
{
  "data": {
    "product": {
      "brand": {
        "name": "string"
      }
    }
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
069A24AC99B558177CB5DCB4CE7E955A0281B31A
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
        "document": "query Test_1 { product { price brand { name } } }",
        "selectionSetId": 0
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

