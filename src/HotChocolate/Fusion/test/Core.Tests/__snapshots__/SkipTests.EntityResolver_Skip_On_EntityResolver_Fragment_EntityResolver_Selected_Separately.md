# EntityResolver_Skip_On_EntityResolver_Fragment_EntityResolver_Selected_Separately

## Result

```json
{
  "data": {
    "productById": {
      "id": "1",
      "name": "string"
    }
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
  productById(id: "1") {
    id
    name
  }
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
FD8123BBA58871A09E0E6F6C118EE453FBD7046F
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) productById(id: \u00221\u0022) { id name } } fragment Test on Query { productById(id: \u00221\u0022) { id name } }",
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

