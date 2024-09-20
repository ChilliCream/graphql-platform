# EntityResolver_Skip_On_EntityResolver_Other_RootField_Selected

## Result

```json
{
  "data": {
    "productById": {
      "id": "1",
      "name": "string"
    },
    "other": "string"
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") @skip(if: $skip) {
    id
    name
  }
  other
}
```

## QueryPlan Hash

```text
9C12237B1A40129B239D76EA6B2F29D0D1305240
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) @skip(if: $skip) { id name } other }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { productById(id: \u00221\u0022) @skip(if: $skip) { id name } other }",
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

