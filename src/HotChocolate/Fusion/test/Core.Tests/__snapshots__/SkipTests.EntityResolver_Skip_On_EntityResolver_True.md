# EntityResolver_Skip_On_EntityResolver

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  productById(id: "1") @skip(if: $skip) {
    id
    name
  }
}
```

## QueryPlan Hash

```text
349C194994A0E867AC52596C3042DCA90B529A8B
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { productById(id: \u00221\u0022) @skip(if: $skip) { id name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1($skip: Boolean!) { productById(id: \u00221\u0022) @skip(if: $skip) { id name } }",
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

