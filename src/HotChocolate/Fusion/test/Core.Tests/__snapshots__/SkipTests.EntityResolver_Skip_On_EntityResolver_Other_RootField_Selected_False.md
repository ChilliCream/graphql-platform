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
FD935DE55C30F33FFEE00968D558446067817DE3
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
        "document": "query Test_1 { productById(id: \u00221\u0022) { id name } other }",
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

