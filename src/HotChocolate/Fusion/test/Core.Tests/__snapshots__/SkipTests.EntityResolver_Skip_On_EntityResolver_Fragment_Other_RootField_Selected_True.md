# EntityResolver_Skip_On_EntityResolver_Fragment_Other_RootField_Selected

## Result

```json
{
  "data": {
    "other": "string"
  }
}
```

## Request

```graphql
query Test($skip: Boolean!) {
  ... Test @skip(if: $skip)
  other
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
82F401D4DDCF4CA99A4426A0DBAB4D69CD3F13A2
```

## QueryPlan

```json
{
  "document": "query Test($skip: Boolean!) { ... Test @skip(if: $skip) other } fragment Test on Query { productById(id: \u00221\u0022) { id name } }",
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

