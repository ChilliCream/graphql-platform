# Same_TopLevel_Selection_Skipped_With_Entirely_Different_Skips

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip1: Boolean!, $skip2: Boolean!) {
  product @skip(if: $skip1) {
    price
  }
  product @skip(if: $skip2) {
    brand {
      name
    }
  }
}
```

## QueryPlan Hash

```text
4E267E255CE78944D042F593B46CF371F5ACDE78
```

## QueryPlan

```json
{
  "document": "query Test($skip1: Boolean!, $skip2: Boolean!) { product @skip(if: $skip1) { price } product @skip(if: $skip2) { brand { name } } }",
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

