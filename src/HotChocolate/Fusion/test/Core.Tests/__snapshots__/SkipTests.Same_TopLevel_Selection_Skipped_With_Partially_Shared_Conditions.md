# Same_TopLevel_Selection_Skipped_With_Partially_Shared_Conditions

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!, $include: Boolean!) {
  product @skip(if: $skip) {
    price
  }
  product @include(if: $include) @skip(if: $skip) {
    brand {
      name
    }
  }
}
```

## QueryPlan Hash

```text
CD4B630431125928F20FCE53B019898FAF35C9BD
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!, $include: Boolean!) { product @skip(if: $skip) { price } product @include(if: $include) @skip(if: $skip) { brand { name } } }",
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

