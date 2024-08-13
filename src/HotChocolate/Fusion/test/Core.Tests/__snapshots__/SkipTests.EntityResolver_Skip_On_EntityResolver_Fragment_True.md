# EntityResolver_Skip_On_EntityResolver_Fragment

## Result

```json
{
  "data": {}
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
}

fragment Test on Query {
  productById(id: "1") {
    id
    name
  }
}
```

## QueryPlan Hash

```text
299C3A16E021343840D26468051FDFFA6D9D7878
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) } fragment Test on Query { productById(id: \u00221\u0022) { id name } }",
  "operation": "Test",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query Test_1 { productById(id: \u00221\u0022) { id name } }",
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

